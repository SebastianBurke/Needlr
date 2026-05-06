using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Persistence.TrustSafety;

/// <summary>
/// Computes the public behavioral-signal numbers from booking history. v1 runs the queries
/// inline on each <c>GET /api/artists/{id}</c> hit; if profiling shows pressure later, this
/// is the natural place to introduce a 1-minute cache.
/// </summary>
internal sealed class BehavioralSignalsService(NeedlrDbContext db, IClock clock) : IBehavioralSignalsService
{
    public const int CompletionMinSample = 10;
    public const int HealedPhotoMinSample = 10;
    public const int RepeatClientMinSample = 20;

    private readonly NeedlrDbContext _db = db;

    public async Task<BehavioralSignals> ComputeAsync(Guid artistId, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        return new BehavioralSignals(
            ResponseMedianHours: await ComputeResponseMedianHoursAsync(artistId, now, cancellationToken),
            CompletionRatePercent: await ComputeCompletionRateAsync(artistId, now, cancellationToken),
            HealedPhotoRatePercent: await ComputeHealedPhotoRateAsync(artistId, now, cancellationToken),
            RepeatClientRatePercent: await ComputeRepeatClientRateAsync(artistId, now, cancellationToken));
    }

    private async Task<double?> ComputeResponseMedianHoursAsync(
        Guid artistId, DateTime now, CancellationToken cancellationToken)
    {
        // First-action timestamp = AcceptedAt for Accepted+; we don't store a dedicated
        // DeclineAt yet, so v1 only counts Accepted bookings. The window keys on RequestedAt
        // so a recently-accepted booking from an old request still counts.
        var since = now.AddDays(-30);
        var rows = await _db.Bookings
            .Where(b => b.ArtistId == artistId
                && b.AcceptedAt != null
                && b.RequestedAt >= since)
            .Select(b => new { b.RequestedAt, AcceptedAt = b.AcceptedAt!.Value })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return null;
        var hours = rows
            .Select(r => (r.AcceptedAt - r.RequestedAt).TotalHours)
            .OrderBy(h => h)
            .ToList();
        return Median(hours);
    }

    private async Task<double?> ComputeCompletionRateAsync(
        Guid artistId, DateTime now, CancellationToken cancellationToken)
    {
        var since = now.AddDays(-90);

        // Denominator: bookings that ever reached Confirmed-or-better over the last 90 days
        // (window keyed on AcceptedAt since that's the entry into the post-acceptance chain).
        var totalConfirmed = await _db.Bookings.CountAsync(b =>
            b.ArtistId == artistId
            && b.AcceptedAt != null
            && b.AcceptedAt >= since, cancellationToken);
        if (totalConfirmed < CompletionMinSample) return null;

        var completed = await _db.Bookings.CountAsync(b =>
            b.ArtistId == artistId
            && b.AcceptedAt != null
            && b.AcceptedAt >= since
            && b.Status == BookingStatus.Completed, cancellationToken);

        return Math.Round(completed * 100.0 / totalConfirmed, 1);
    }

    private async Task<double?> ComputeHealedPhotoRateAsync(
        Guid artistId, DateTime now, CancellationToken cancellationToken)
    {
        var fourMonthsAgo = now.AddMonths(-4);
        var eligibleBookingIds = await _db.Bookings
            .Where(b => b.ArtistId == artistId
                && b.Status == BookingStatus.Completed
                && b.CompletedAt != null
                && b.CompletedAt <= fourMonthsAgo)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        if (eligibleBookingIds.Count < HealedPhotoMinSample) return null;

        // A healed photo exists if there's a SessionPhoto on a portfolio piece linked to
        // the booking with PhotoType=Healed (uploaded by the customer).
        var withHealed = await _db.SessionPhotos
            .Where(p => p.PhotoType == PhotoType.Healed
                && _db.PortfolioPieces.Any(pp => pp.Id == p.PortfolioPieceId
                    && pp.LinkedBookingId != null
                    && eligibleBookingIds.Contains(pp.LinkedBookingId.Value)))
            .Select(p => p.PortfolioPieceId)
            .Distinct()
            .CountAsync(cancellationToken);

        return Math.Round(withHealed * 100.0 / eligibleBookingIds.Count, 1);
    }

    private async Task<double?> ComputeRepeatClientRateAsync(
        Guid artistId, DateTime now, CancellationToken cancellationToken)
    {
        var since = now.AddMonths(-12);

        var customerCounts = await _db.Bookings
            .Where(b => b.ArtistId == artistId
                && b.Status == BookingStatus.Completed
                && b.CompletedAt != null
                && b.CompletedAt >= since)
            .GroupBy(b => b.CustomerId)
            .Select(g => g.Count())
            .ToListAsync(cancellationToken);

        if (customerCounts.Count < RepeatClientMinSample) return null;

        var repeats = customerCounts.Count(c => c >= 2);
        return Math.Round(repeats * 100.0 / customerCounts.Count, 1);
    }

    private static double Median(IReadOnlyList<double> sorted)
    {
        if (sorted.Count == 0) return 0;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}

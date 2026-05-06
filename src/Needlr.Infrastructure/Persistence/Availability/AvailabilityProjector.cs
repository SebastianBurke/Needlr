using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Availability;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Persistence.Availability;

/// <summary>
/// Computes ArtistAvailabilityProjection rows by combining recurring patterns, one-off
/// overrides, optional booking windows, and capacity-consuming bookings (FEATURE_SPECS.md
/// § Availability). Writes through the unit of work — caller commits.
/// </summary>
internal sealed class AvailabilityProjector(
    IAvailabilityPatternRepository patterns,
    IAvailabilityOverrideRepository overrides,
    IBookingWindowRepository windows,
    IBookingRepository bookings,
    IArtistAvailabilityProjectionRepository projections,
    IClock clock) : IAvailabilityProjector
{
    /// <summary>
    /// Length of the rolling projection window used by the discovery filter. 90 days mirrors
    /// the FEATURE_SPECS contract — long enough to cover the customer-side date picker
    /// without exploding row counts for artists with sparse availability.
    /// </summary>
    public const int RollingWindowDays = 90;

    public Task RebuildRollingWindowAsync(Guid artistId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        return RebuildAsync(artistId, today, today.AddDays(RollingWindowDays - 1), cancellationToken);
    }

    public async Task RebuildAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from)
            throw new ArgumentException("`to` must be on or after `from`.", nameof(to));

        // Snapshot inputs once so the per-day loop is in-memory only.
        var artistPatterns = await patterns.ListByArtistAsync(artistId, cancellationToken);
        var artistOverrides = await overrides.ListByArtistAsync(artistId, from, to, cancellationToken);
        var artistWindows = await windows.ListByArtistAsync(artistId, cancellationToken);
        var consuming = await bookings.ListConsumingForArtistInWindowAsync(artistId, from, to, cancellationToken);

        await projections.DeleteWindowAsync(artistId, from, to, cancellationToken);

        var overridesByDate = artistOverrides.ToDictionary(o => o.Date);
        var bookedHoursByDate = consuming
            .Where(b => b.ConfirmedSessionDate.HasValue)
            .GroupBy(b => DateOnly.FromDateTime(b.ConfirmedSessionDate!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(b => b.EstimatedDurationHours));

        var now = clock.UtcNow;
        var hasAnyWindow = artistWindows.Count > 0;

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var (status, capacityHours) = ResolveDay(date, artistPatterns, overridesByDate);
            bookedHoursByDate.TryGetValue(date, out var consumed);
            var remaining = Math.Max(0m, capacityHours - consumed);

            var windowOpen = !hasAnyWindow ||
                artistWindows.Any(w =>
                    date >= w.TargetRangeStart
                    && date <= w.TargetRangeEnd
                    && now >= w.WindowOpensAt
                    && now <= w.WindowClosesAt);

            var bookable = status is AvailabilityStatus.Available or AvailabilityStatus.Limited
                && remaining > 0
                && windowOpen;

            projections.Add(new ArtistAvailabilityProjection(
                id: Guid.NewGuid(),
                artistId: artistId,
                date: date,
                isBookable: bookable,
                remainingSessionHours: remaining,
                recomputedAt: now));
        }
    }

    /// <summary>
    /// Resolve the day's effective status + capacity. Overrides win outright over patterns;
    /// in their absence, the most-recently-effective pattern row for that DayOfWeek applies
    /// (multiple effective windows can coexist, and the projector picks the latest one whose
    /// EffectiveFrom is on or before <paramref name="date"/>).
    /// </summary>
    private static (AvailabilityStatus Status, decimal CapacityHours) ResolveDay(
        DateOnly date,
        IReadOnlyList<AvailabilityPattern> patterns,
        Dictionary<DateOnly, AvailabilityOverride> overridesByDate)
    {
        if (overridesByDate.TryGetValue(date, out var ovr))
            return (ovr.Status, CapacityFor(ovr.Status, ovr.MaxSessionHours));

        AvailabilityPattern? best = null;
        foreach (var p in patterns)
        {
            if (p.DayOfWeek != date.DayOfWeek) continue;
            if (p.EffectiveFrom > date) continue;
            if (p.EffectiveUntil is { } u && u < date) continue;
            if (best is null || p.EffectiveFrom > best.EffectiveFrom) best = p;
        }

        if (best is null)
            return (AvailabilityStatus.Closed, 0m);
        return (best.Status, CapacityFor(best.Status, best.MaxSessionHours));
    }

    private const decimal DefaultAvailableHours = 8m;
    private const decimal DefaultLimitedHours = 3m;

    /// <summary>Translate a status + optional cap into the day's working capacity.</summary>
    private static decimal CapacityFor(AvailabilityStatus status, decimal? maxSessionHours) => status switch
    {
        AvailabilityStatus.Closed => 0m,
        AvailabilityStatus.Available => maxSessionHours ?? DefaultAvailableHours,
        AvailabilityStatus.Limited => maxSessionHours ?? DefaultLimitedHours,
        _ => 0m
    };
}

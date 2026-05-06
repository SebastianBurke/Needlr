using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Application.TrustSafety.GetTrustSafetyDashboard;
using Needlr.Domain.Bookings;

namespace Needlr.Infrastructure.Persistence.TrustSafety;

internal sealed class TrustSafetyDashboardService(NeedlrDbContext db) : ITrustSafetyDashboardService
{
    public const int LowAverageMinFeedbacks = 3;
    public const double LowAverageThreshold = 3.0;
    public const int LowAverageWindowSize = 10;
    public const int RepeatNotBookingAgainThreshold = 2;

    /// <summary>
    /// Initial v1 list — narrow on purpose; admin tooling in Phase 22 surfaces these for
    /// human review rather than auto-actioning. Add words via PR + audit signoff.
    /// </summary>
    private static readonly string[] SafetyKeywords =
    [
        "harassment",
        "harass",
        "unsafe",
        "unhygienic",
        "no consent",
        "non-consensual",
        "assault",
        "stalker",
        "threatening",
        "drunk",
    ];

    private readonly NeedlrDbContext _db = db;

    public async Task<TrustSafetyDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        // Pull recent feedback per artist; aggregate in-memory because EF's GroupBy +
        // window-aware Take is awkward, and the v1 volume is small.
        var allFeedback = await _db.BookingFeedbacks
            .Join(_db.Bookings, f => f.BookingId, b => b.Id, (f, b) => new { f, b.ArtistId })
            .OrderByDescending(x => x.f.SubmittedAt)
            .Select(x => new { x.f, x.ArtistId })
            .ToListAsync(cancellationToken);

        var byArtist = allFeedback
            .GroupBy(x => x.ArtistId)
            .ToDictionary(g => g.Key, g => g.Take(LowAverageWindowSize).Select(x => x.f).ToList());

        var artistMeta = await _db.Artists
            .Where(a => byArtist.Keys.Contains(a.Id))
            .Select(a => new { a.Id, a.DisplayName })
            .ToListAsync(cancellationToken);
        var artistDisplayName = artistMeta.ToDictionary(a => a.Id, a => a.DisplayName);

        var lowAverages = byArtist
            .Where(kv => kv.Value.Count >= LowAverageMinFeedbacks)
            .Select(kv =>
            {
                var avg = kv.Value.Average(f => Avg3(f));
                return (ArtistId: kv.Key, Count: kv.Value.Count, Average: avg);
            })
            .Where(t => t.Average < LowAverageThreshold)
            .OrderBy(t => t.Average)
            .Select(t => new FlaggedArtistDto(
                t.ArtistId,
                artistDisplayName.GetValueOrDefault(t.ArtistId, ""),
                t.Count,
                Math.Round(t.Average, 2)))
            .ToList();

        var repeats = byArtist
            .Select(kv => (ArtistId: kv.Key,
                Count: kv.Value.Count,
                NotAgainCount: kv.Value.Count(f => !f.WouldBookAgain)))
            .Where(t => t.NotAgainCount >= RepeatNotBookingAgainThreshold)
            .OrderByDescending(t => t.NotAgainCount)
            .Select(t => new FlaggedArtistDto(
                t.ArtistId,
                artistDisplayName.GetValueOrDefault(t.ArtistId, ""),
                t.Count,
                Math.Round(t.Count == 0 ? 0 : t.NotAgainCount * 100.0 / t.Count, 1)))
            .ToList();

        var allFreeText = allFeedback
            .Where(x => !string.IsNullOrWhiteSpace(x.f.FreeText))
            .ToList();
        var keywordMatches = new List<FlaggedFeedbackDto>();
        foreach (var x in allFreeText)
        {
            var hit = SafetyKeywords.FirstOrDefault(k =>
                x.f.FreeText!.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (hit is null) continue;
            keywordMatches.Add(new FlaggedFeedbackDto(
                x.f.Id,
                x.f.BookingId,
                x.ArtistId,
                artistDisplayName.GetValueOrDefault(x.ArtistId, ""),
                x.f.SubmittedAt,
                hit,
                x.f.FreeText!));
        }

        return new TrustSafetyDashboardDto(lowAverages, repeats, keywordMatches);
    }

    private static double Avg3(BookingFeedback f) =>
        (f.CommunicationRating + f.CleanlinessRating + f.RespectedDesignBriefRating) / 3.0;
}

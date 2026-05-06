using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Hangfire-friendly recurring job that rebuilds the rolling-window availability projection
/// for every artist. Phase 9 introduces the job class; Phase 14 wires
/// <c>RecurringJob.AddOrUpdate</c> with the 3 AM cron + <c>AddHangfireServer</c>.
/// </summary>
public sealed class RebuildAllAvailabilityProjectionsRecurringJob(
    NeedlrDbContext db,
    IAvailabilityProjector projector,
    ILogger<RebuildAllAvailabilityProjectionsRecurringJob> logger)
{
    public const string JobId = "rebuild-all-availability-projections";

    /// <summary>
    /// Iterates every artist, persisting per-artist so a single failure doesn't lose work
    /// for the rest of the roster. The projector writes via the same DbContext, so we
    /// SaveChanges at the end of each artist.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var artistIds = await db.Artists
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var failures = 0;
        foreach (var artistId in artistIds)
        {
            try
            {
                await projector.RebuildRollingWindowAsync(artistId, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Failed to rebuild availability projection for artist {ArtistId}", artistId);
            }
        }

        logger.LogInformation(
            "Rebuilt availability projections for {Total} artist(s); {Failures} failure(s).",
            artistIds.Count, failures);
    }
}

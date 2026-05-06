namespace Needlr.Application.Abstractions;

/// <summary>
/// Computes <c>ArtistAvailabilityProjection</c> rows for an artist over a date range by
/// resolving recurring patterns, one-off overrides, optional booking windows, and
/// capacity-consuming bookings. The discovery availability filter and the nightly Hangfire
/// job depend on this — see <c>docs/FEATURE_SPECS.md</c> § Availability.
/// </summary>
public interface IAvailabilityProjector
{
    /// <summary>
    /// Replace the projection rows for <paramref name="artistId"/> within [from, to] with
    /// freshly-computed values. Caller is responsible for committing the unit of work
    /// (handlers running in the MediatR transaction pipeline get this for free).
    /// </summary>
    Task RebuildAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience: rebuild the rolling 90-day window starting today. Used by the on-demand
    /// command and the nightly job.
    /// </summary>
    Task RebuildRollingWindowAsync(Guid artistId, CancellationToken cancellationToken = default);
}

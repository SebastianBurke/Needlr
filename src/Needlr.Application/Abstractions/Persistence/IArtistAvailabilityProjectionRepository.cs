using Needlr.Domain.Availability;

namespace Needlr.Application.Abstractions.Persistence;

/// <summary>
/// Persistence access for the denormalized <see cref="ArtistAvailabilityProjection"/> table.
/// The projector replaces the rolling 90-day window for an artist by deleting the existing
/// window and inserting fresh rows. Reads serve discovery.
/// </summary>
public interface IArtistAvailabilityProjectionRepository
{
    Task<IReadOnlyList<ArtistAvailabilityProjection>> ListAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Delete every projection row for an artist within [from, to] (inclusive).</summary>
    Task DeleteWindowAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    void Add(ArtistAvailabilityProjection projection);
}

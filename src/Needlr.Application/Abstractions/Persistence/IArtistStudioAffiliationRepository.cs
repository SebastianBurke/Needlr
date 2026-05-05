using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Application.Abstractions.Persistence;

public interface IArtistStudioAffiliationRepository
{
    Task<ArtistStudioAffiliation?> GetByIdAsync(Guid affiliationId, CancellationToken cancellationToken = default);

    Task<ArtistStudioAffiliation?> GetByArtistAndStudioAsync(
        Guid artistId, Guid studioId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtistStudioAffiliation>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default);

    /// <summary>Lists affiliations on a studio. Filter by status (e.g., Active for the public roster).</summary>
    Task<IReadOnlyList<ArtistStudioAffiliation>> ListByStudioAsync(
        Guid studioId, AffiliationStatus? status = null, CancellationToken cancellationToken = default);

    void Add(ArtistStudioAffiliation affiliation);
    void Remove(ArtistStudioAffiliation affiliation);

    /// <summary>True if <paramref name="artistId"/> currently holds Founder or Admin role on the studio with Active status.</summary>
    Task<bool> IsAdminAsync(Guid artistId, Guid studioId, CancellationToken cancellationToken = default);
}

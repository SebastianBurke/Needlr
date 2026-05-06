using Needlr.Domain.Identity;

namespace Needlr.Application.Abstractions.Persistence;

public interface IArtistLeadTimeRepository
{
    Task<IReadOnlyList<ArtistLeadTime>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default);

    void Add(ArtistLeadTime leadTime);
    void Remove(ArtistLeadTime leadTime);
}

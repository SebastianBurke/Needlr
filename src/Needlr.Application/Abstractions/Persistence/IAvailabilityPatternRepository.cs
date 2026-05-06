using Needlr.Domain.Availability;

namespace Needlr.Application.Abstractions.Persistence;

public interface IAvailabilityPatternRepository
{
    Task<IReadOnlyList<AvailabilityPattern>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default);

    void Add(AvailabilityPattern pattern);
    void Remove(AvailabilityPattern pattern);
    void RemoveRange(IEnumerable<AvailabilityPattern> patterns);
}

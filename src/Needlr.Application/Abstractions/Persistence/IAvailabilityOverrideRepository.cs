using Needlr.Domain.Availability;

namespace Needlr.Application.Abstractions.Persistence;

public interface IAvailabilityOverrideRepository
{
    Task<AvailabilityOverride?> GetAsync(
        Guid artistId, DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityOverride>> ListByArtistAsync(
        Guid artistId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    void Add(AvailabilityOverride availabilityOverride);
    void Remove(AvailabilityOverride availabilityOverride);
}

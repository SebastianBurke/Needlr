using Needlr.Domain.Portfolio;

namespace Needlr.Application.Abstractions.Persistence;

public interface ITattooStyleRepository
{
    Task<IReadOnlyList<TattooStyle>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TattooStyle>> ListCanonicalAsync(CancellationToken cancellationToken = default);
}

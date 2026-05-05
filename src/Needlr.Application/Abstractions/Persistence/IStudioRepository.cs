using Needlr.Domain.Studios;

namespace Needlr.Application.Abstractions.Persistence;

public interface IStudioRepository
{
    Task<Studio?> GetByIdAsync(Guid studioId, CancellationToken cancellationToken = default);

    /// <summary>Loads a studio with its affiliations eagerly loaded — used by admin/roster handlers.</summary>
    Task<Studio?> GetByIdWithAffiliationsAsync(Guid studioId, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive name search; returns up to <paramref name="take"/> matches.</summary>
    Task<IReadOnlyList<Studio>> SearchByNameAsync(string query, int take, CancellationToken cancellationToken = default);

    void Add(Studio studio);
}

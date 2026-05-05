using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Portfolio;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class TattooStyleRepository(NeedlrDbContext db) : ITattooStyleRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<TattooStyle>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return [];
        return await _db.TattooStyles
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TattooStyle>> ListCanonicalAsync(CancellationToken cancellationToken = default) =>
        await _db.TattooStyles
            .Where(s => s.IsCanonical)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
}

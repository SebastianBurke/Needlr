using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Studios;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class StudioRepository(NeedlrDbContext db) : IStudioRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<Studio?> GetByIdAsync(Guid studioId, CancellationToken cancellationToken = default) =>
        _db.Studios.FirstOrDefaultAsync(s => s.Id == studioId, cancellationToken);

    public Task<Studio?> GetByIdWithAffiliationsAsync(Guid studioId, CancellationToken cancellationToken = default) =>
        _db.Studios
            .Include(s => s.Affiliations)
            .FirstOrDefaultAsync(s => s.Id == studioId, cancellationToken);

    public async Task<IReadOnlyList<Studio>> SearchByNameAsync(string query, int take, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var pattern = $"%{query.Trim()}%";
        var rows = await _db.Studios
            .Where(s => EF.Functions.ILike(s.Name, pattern))
            .OrderBy(s => s.Name)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public void Add(Studio studio) => _db.Studios.Add(studio);
}

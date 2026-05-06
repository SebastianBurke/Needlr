using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Moderation;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class UserWarningRepository(NeedlrDbContext db) : IUserWarningRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<UserWarning>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _db.UserWarnings
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.IssuedAt)
            .ToListAsync(cancellationToken);

    public void Add(UserWarning warning) => _db.UserWarnings.Add(warning);
}

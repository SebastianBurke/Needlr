using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Identity;

internal sealed class ModerationService(
    NeedlrDbContext db,
    IClock clock) : IModerationService
{
    public Task<bool> IsSuspendedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SuspendedAt != null)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> SuspendAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return false;
        if (user.SuspendedAt is not null) return true; // idempotent
        user.SuspendedAt = clock.UtcNow;
        user.SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return true;
    }

    public async Task<bool> UnsuspendAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return false;
        user.SuspendedAt = null;
        user.SuspensionReason = null;
        return true;
    }
}

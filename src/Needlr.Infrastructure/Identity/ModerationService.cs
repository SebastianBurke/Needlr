using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Moderation.SearchUsers;
using Needlr.Domain.Enums;
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

    public async Task<PagedResult<AdminUserDto>> SearchUsersAsync(
        string? emailSubstring,
        UserRole? role,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();
        IQueryable<ApplicationUser> q = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(emailSubstring))
        {
            var needle = emailSubstring.Trim().ToLower();
            q = q.Where(u => u.Email != null && u.Email.ToLower().Contains(needle));
        }
        if (role is { } r)
        {
            q = q.Where(u => u.Role == r);
        }

        var total = await q.CountAsync(cancellationToken);

        // Project + LEFT JOIN both profile tables so display name fills in when present.
        // EF can't translate two outer-applies to a single column directly, so we do
        // GroupJoin + DefaultIfEmpty for each side.
        var rows = await q
            .OrderBy(u => u.Email)
            .Skip(p.Skip).Take(p.PageSize)
            .GroupJoin(
                db.CustomerProfiles.AsNoTracking(),
                u => u.Id,
                cp => cp.UserId,
                (u, cps) => new { u, cps })
            .SelectMany(x => x.cps.DefaultIfEmpty(), (x, cp) => new { x.u, cp })
            .GroupJoin(
                db.Artists.AsNoTracking(),
                x => x.u.Id,
                a => a.UserId,
                (x, ars) => new { x.u, x.cp, ars })
            .SelectMany(x => x.ars.DefaultIfEmpty(), (x, ar) => new
            {
                x.u.Id,
                x.u.Email,
                x.u.Role,
                CustomerName = x.cp != null ? x.cp.DisplayName : null,
                ArtistName = ar != null ? ar.DisplayName : null,
                x.u.CreatedAt,
                x.u.SuspendedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminUserDto(
            r.Id,
            r.Email ?? string.Empty,
            r.Role,
            r.CustomerName ?? r.ArtistName,
            r.CreatedAt,
            r.SuspendedAt)).ToList();

        return new PagedResult<AdminUserDto>(items, p.Page, p.PageSize, total);
    }
}

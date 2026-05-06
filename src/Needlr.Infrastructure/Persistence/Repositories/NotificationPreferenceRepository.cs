using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Domain.Notifications;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class NotificationPreferenceRepository(NeedlrDbContext db) : INotificationPreferenceRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _db.NotificationPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Type)
            .ToListAsync(cancellationToken);

    public Task<NotificationPreference?> GetAsync(
        Guid userId, NotificationType type, CancellationToken cancellationToken = default) =>
        _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, cancellationToken);

    public void Add(NotificationPreference preference) =>
        _db.NotificationPreferences.Add(preference);

    public void Remove(NotificationPreference preference) =>
        _db.NotificationPreferences.Remove(preference);
}

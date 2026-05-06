using Needlr.Domain.Enums;
using Needlr.Domain.Notifications;

namespace Needlr.Application.Abstractions.Persistence;

public interface INotificationPreferenceRepository
{
    Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the explicit (EmailEnabled, PushEnabled) row for (user, type), or null when
    /// the user has never overridden the platform default. The dispatcher's resolver treats
    /// null as (true, true) — opt-out, not opt-in.
    /// </summary>
    Task<NotificationPreference?> GetAsync(
        Guid userId, NotificationType type, CancellationToken cancellationToken = default);

    void Add(NotificationPreference preference);
    void Remove(NotificationPreference preference);
}

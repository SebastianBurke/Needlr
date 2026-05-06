using Needlr.Domain.Enums;

namespace Needlr.Domain.Notifications;

/// <summary>
/// One row per (UserId, NotificationType). Storing only the explicit overrides — missing
/// rows resolve to the platform default (<c>true</c> for both channels in v1). Cuts table
/// width compared to a one-row-per-user-with-32-columns design and lets us add new
/// notification types without schema changes.
/// </summary>
public sealed class NotificationPreference
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public NotificationType Type { get; init; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }

    private NotificationPreference() { }

    public NotificationPreference(
        Guid id, Guid userId, NotificationType type, bool emailEnabled, bool pushEnabled)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));

        Id = id;
        UserId = userId;
        Type = type;
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
    }
}

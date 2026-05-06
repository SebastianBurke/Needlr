using Needlr.Application.Messaging;

namespace Needlr.Application.Notifications.UpdateNotificationPreferences;

/// <summary>
/// Bulk-replace the calling user's preference rows. Supplied items overwrite any existing
/// rows for the same <c>NotificationType</c>; types not in the list keep their stored
/// override (or, if none, fall back to the platform default of "all on" at dispatch time).
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(
    IReadOnlyList<NotificationPreferenceDto> Preferences) : ICommand;

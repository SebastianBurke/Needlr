using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Notifications.GetMyNotificationPreferences;

/// <summary>
/// Returns one row per <c>NotificationType</c>, with the user's stored override or the
/// platform default ("all on") filled in for any type they haven't customized.
/// </summary>
public sealed record GetMyNotificationPreferencesQuery
    : IQuery<IReadOnlyList<NotificationPreferenceDto>>;

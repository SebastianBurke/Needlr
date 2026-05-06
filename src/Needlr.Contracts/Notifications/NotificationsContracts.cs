namespace Needlr.Contracts.Notifications;

// ---- Preferences ----

public sealed record NotificationPreferenceRequestItem(
    string Type,           // wire-format string of NotificationType
    bool EmailEnabled,
    bool PushEnabled);

public sealed record UpdateNotificationPreferencesRequest(
    IReadOnlyList<NotificationPreferenceRequestItem> Preferences);

public sealed record NotificationPreferenceResponse(
    string Type,
    bool EmailEnabled,
    bool PushEnabled);

public sealed record NotificationPreferencesResponse(
    IReadOnlyList<NotificationPreferenceResponse> Items);

// ---- Push subscriptions ----

public sealed record RegisterPushSubscriptionRequest(
    string Endpoint, string P256dh, string Auth);

public sealed record PushSubscriptionResponse(
    Guid Id,
    string Endpoint,
    DateTime CreatedAt,
    DateTime? LastSentAt);

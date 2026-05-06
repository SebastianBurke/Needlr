using Needlr.Domain.Enums;

namespace Needlr.Application.Notifications;

public sealed record NotificationPreferenceDto(
    NotificationType Type,
    bool EmailEnabled,
    bool PushEnabled);

public sealed record PushSubscriptionDto(
    Guid Id,
    string Endpoint,
    DateTime CreatedAt,
    DateTime? LastSentAt);

namespace Needlr.Domain.Enums;

/// <summary>
/// Per-channel toggle keys from FEATURE_SPECS.md § Notifications. Each user has at most
/// one <c>NotificationPreference</c> row per type; missing rows mean "use platform defaults"
/// (which is "on" for every channel for v1).
/// </summary>
public enum NotificationType
{
    NewBookingRequest = 1,
    BookingAccepted = 2,
    BookingDeclined = 3,
    BookingExpired = 4,
    NewMessage = 5,
    BookingReminder24h = 6,
    HealedPhotoPrompt = 7,
    CredentialExpiring30d = 8,
    CredentialExpiring7d = 9,
    CredentialExpired = 10,
    VerificationApproved = 11,
    VerificationRejected = 12,
    StudioJoinRequest = 13,
    StudioJoinResolved = 14,
    GuestSpotInvitation = 15,
    GuestSpotResolved = 16,
}

/// <summary>
/// Channel a notification fires on. Email is always available; Web Push only fires when
/// the user has at least one <c>PushSubscription</c> registered for the calling browser.
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    Push = 2,
}

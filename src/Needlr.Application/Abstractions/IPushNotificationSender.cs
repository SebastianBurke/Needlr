namespace Needlr.Application.Abstractions;

/// <summary>
/// Sends Web Push notifications to PWA-installed clients. VAPID-signed; see Phase 13.
/// </summary>
public interface IPushNotificationSender
{
    Task SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// A single Web Push subscription as registered by the browser PushSubscription API.
/// </summary>
public sealed record PushSubscription(string Endpoint, string P256dh, string Auth);

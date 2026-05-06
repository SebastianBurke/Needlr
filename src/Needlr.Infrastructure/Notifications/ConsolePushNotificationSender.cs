using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Dev / fallback push sender — logs the payload. Used whenever VAPID keys are not
/// configured. The wire-format <c>WebPushNotificationSender</c> arrives behind a future
/// flag once we land an actual Web Push library; this stub keeps the dispatcher simple in
/// the meantime.
/// </summary>
internal sealed class ConsolePushNotificationSender(
    ILogger<ConsolePushNotificationSender> logger) : IPushNotificationSender
{
    public Task SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Push dispatch (console fallback) | endpoint={Endpoint} payload=\"{Payload}\"",
            subscription.Endpoint, payload);
        return Task.CompletedTask;
    }
}

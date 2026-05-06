using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions;

/// <summary>
/// High-level "send notification X to user Y" entry point. Resolves the user's per-channel
/// preferences (defaults to on when no row exists), then fans out to <c>IEmailSender</c>
/// and <c>IPushNotificationSender</c>. Errors on individual channels are logged but never
/// fail the caller — notification dispatch is best-effort and must not interfere with the
/// underlying business operation.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid userId,
        NotificationType type,
        NotificationContent content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Channel-agnostic content payload. The dispatcher passes <see cref="EmailSubject"/> +
/// <see cref="EmailBody"/> to the email sender, and the JSON payload built from
/// <see cref="PushTitle"/> + <see cref="PushBody"/> to the push sender.
/// </summary>
public sealed record NotificationContent(
    string EmailSubject,
    string EmailBody,
    string PushTitle,
    string PushBody);

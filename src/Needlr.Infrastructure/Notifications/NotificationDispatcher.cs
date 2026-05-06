using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using AppPushSubscription = Needlr.Application.Abstractions.PushSubscription;

namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Resolves preferences + fans out to email/push channels. Best-effort: a failure on one
/// channel logs but doesn't propagate, so a transient SendGrid hiccup never breaks an
/// AcceptBookingCommand. Per FEATURE_SPECS § Notifications, missing preference rows
/// resolve to "on" for every channel; users opt out, never opt in.
/// </summary>
internal sealed class NotificationDispatcher(
    NeedlrDbContext db,
    INotificationPreferenceRepository preferences,
    IPushSubscriptionRepository subscriptions,
    IEmailSender emailSender,
    IPushNotificationSender pushSender,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task DispatchAsync(
        Guid userId,
        NotificationType type,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        var pref = await preferences.GetAsync(userId, type, cancellationToken);
        var emailEnabled = pref?.EmailEnabled ?? true;
        var pushEnabled = pref?.PushEnabled ?? true;

        if (emailEnabled)
            await TrySendEmailAsync(userId, content, cancellationToken);

        if (pushEnabled)
            await TrySendPushAsync(userId, content, cancellationToken);
    }

    private async Task TrySendEmailAsync(Guid userId, NotificationContent content, CancellationToken cancellationToken)
    {
        var email = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(email))
        {
            logger.LogWarning("No email on file for user {UserId}; skipping email dispatch.", userId);
            return;
        }

        try
        {
            await emailSender.SendAsync(email, content.EmailSubject, content.EmailBody, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email dispatch failed for user {UserId}.", userId);
        }
    }

    private async Task TrySendPushAsync(Guid userId, NotificationContent content, CancellationToken cancellationToken)
    {
        var subs = await subscriptions.ListByUserAsync(userId, cancellationToken);
        if (subs.Count == 0) return;

        var payload = JsonSerializer.Serialize(new { title = content.PushTitle, body = content.PushBody });

        foreach (var s in subs)
        {
            try
            {
                await pushSender.SendAsync(
                    new AppPushSubscription(s.Endpoint, s.P256dh, s.Auth),
                    payload,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Push dispatch failed for subscription {SubId}.", s.Id);
            }
        }
    }
}

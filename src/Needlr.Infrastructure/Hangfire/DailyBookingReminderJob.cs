using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Daily sweep that emits 24-hour reminders to both parties for sessions taking place in
/// approximately 24 hours (FEATURE_SPECS.md § Booking lifecycle post-confirmation).
/// One-shot per booking via the <c>ReminderSentAt</c> stamp. Window is generous (now+12h to
/// now+36h) so a booking always catches at least one reminder regardless of when the
/// recurring schedule fires relative to the session time.
/// </summary>
public sealed class DailyBookingReminderJob(
    NeedlrDbContext db,
    IArtistRepository artists,
    INotificationDispatcher notifications,
    IClock clock,
    ILogger<DailyBookingReminderJob> logger)
{
    public const string JobId = "daily-booking-reminder";

    private static readonly BookingStatus[] RemindableStatuses =
    [
        BookingStatus.Accepted,
        BookingStatus.DepositCaptured,
        BookingStatus.Confirmed
    ];

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var lower = now.AddHours(12);
        var upper = now.AddHours(36);

        var due = await db.Bookings
            .Where(b => b.ReminderSentAt == null
                && RemindableStatuses.Contains(b.Status)
                && b.ConfirmedSessionDate != null
                && b.ConfirmedSessionDate >= lower
                && b.ConfirmedSessionDate <= upper)
            .ToListAsync(cancellationToken);

        var sent = 0;
        var failures = 0;
        foreach (var booking in due)
        {
            try
            {
                var artistUser = (await artists.GetByIdAsync(booking.ArtistId, cancellationToken))?.UserId;
                var content = new NotificationContent(
                    EmailSubject: "Reminder: session in ~24 hours",
                    EmailBody: $"Your session is scheduled for {booking.ConfirmedSessionDate:yyyy-MM-dd HH:mm} UTC.",
                    PushTitle: "Session in ~24h",
                    PushBody: $"{booking.ConfirmedSessionDate:yyyy-MM-dd HH:mm} UTC");

                await notifications.DispatchAsync(
                    booking.CustomerId, NotificationType.BookingReminder24h, content, cancellationToken);
                if (artistUser is { } u)
                    await notifications.DispatchAsync(u, NotificationType.BookingReminder24h, content, cancellationToken);

                booking.ReminderSentAt = now;
                sent++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Reminder dispatch failed for booking {BookingId}.", booking.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Booking reminders sent for {Sent}/{Total} booking(s); {Failures} failure(s).",
            sent, due.Count, failures);
    }
}

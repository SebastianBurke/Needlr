using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Daily sweep that prompts customers to upload a healed photo at the 4-month mark
/// (FEATURE_SPECS.md § Photo handling). Fires once per Completed booking via the
/// <c>HealedPhotoPromptedAt</c> idempotency stamp; "approximately 4 months" is "&gt;= 4
/// calendar months ago" so a booking that misses the exact day still gets a prompt the
/// next morning.
/// </summary>
public sealed class DailyHealedPhotoPromptJob(
    NeedlrDbContext db,
    INotificationDispatcher notifications,
    IClock clock,
    ILogger<DailyHealedPhotoPromptJob> logger)
{
    public const string JobId = "daily-healed-photo-prompt";
    public const int PromptAfterMonths = 4;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var cutoff = now.AddMonths(-PromptAfterMonths);

        var due = await db.Bookings
            .Where(b => b.Status == BookingStatus.Completed
                && b.HealedPhotoPromptedAt == null
                && b.CompletedAt != null
                && b.CompletedAt <= cutoff)
            .ToListAsync(cancellationToken);

        var prompted = 0;
        var failures = 0;
        foreach (var booking in due)
        {
            try
            {
                await notifications.DispatchAsync(
                    booking.CustomerId,
                    NotificationType.HealedPhotoPrompt,
                    new NotificationContent(
                        EmailSubject: "Share your healed tattoo",
                        EmailBody: "It's been about 4 months since your session. We'd love a healed photo — it helps future clients see how the tattoo settles.",
                        PushTitle: "Share your healed tattoo",
                        PushBody: "Tap to upload a photo"),
                    cancellationToken);
                booking.HealedPhotoPromptedAt = now;
                prompted++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Healed-photo prompt failed for booking {BookingId}.", booking.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Healed-photo prompts sent for {Sent}/{Total} booking(s); {Failures} failure(s).",
            prompted, due.Count, failures);
    }
}

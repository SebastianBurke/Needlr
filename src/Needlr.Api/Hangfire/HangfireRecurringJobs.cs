using Hangfire;
using Needlr.Infrastructure.Hangfire;

namespace Needlr.Api.Hangfire;

/// <summary>
/// Registers every recurring Hangfire job with its cron schedule per BUILD_PLAN.md
/// § Phase 14. Times are UTC; servers running on America/Montreal local time will see
/// these fire offset accordingly.
/// </summary>
internal static class HangfireRecurringJobs
{
    public static void RegisterAll(IRecurringJobManagerV2 jobs, TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? TimeZoneInfo.Utc;

        // 3:00 AM UTC — rebuild the rolling 90-day availability projection.
        jobs.AddOrUpdate<RebuildAllAvailabilityProjectionsRecurringJob>(
            RebuildAllAvailabilityProjectionsRecurringJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "0 3 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 3:30 AM UTC — purge attachment blobs for terminal-state bookings >= 1y old.
        jobs.AddOrUpdate<NightlyBookingAttachmentPurgeJob>(
            NightlyBookingAttachmentPurgeJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "30 3 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 4:00 AM UTC — credential expiry scan: 30/7/0d warnings, downgrade on expiry.
        jobs.AddOrUpdate<NightlyCredentialExpiryScanJob>(
            NightlyCredentialExpiryScanJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "0 4 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 4:30 AM UTC — expire stale Requested bookings that beat the per-booking schedule.
        jobs.AddOrUpdate<ExpireDueRequestedBookingsRecurringJob>(
            ExpireDueRequestedBookingsRecurringJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "30 4 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 5:00 AM UTC — lock threads whose booking went terminal >= 90d ago and slipped.
        jobs.AddOrUpdate<LockOverdueThreadsRecurringJob>(
            LockOverdueThreadsRecurringJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "0 5 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 9:00 AM UTC — 24-hour booking reminders.
        jobs.AddOrUpdate<DailyBookingReminderJob>(
            DailyBookingReminderJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "0 9 * * *",
            new RecurringJobOptions { TimeZone = tz });

        // 10:00 AM UTC — healed-photo prompts at the 4-month mark.
        jobs.AddOrUpdate<DailyHealedPhotoPromptJob>(
            DailyHealedPhotoPromptJob.JobId,
            j => j.RunAsync(CancellationToken.None),
            "0 10 * * *",
            new RecurringJobOptions { TimeZone = tz });
    }
}

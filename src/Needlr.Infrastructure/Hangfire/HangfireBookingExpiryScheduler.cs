using Hangfire;
using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Bookings.ExpireRequestedBooking;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Schedules a one-off Hangfire job that fires <c>ExpireRequestedBookingCommand</c> at the
/// 7-day cutoff for a freshly-requested booking. The recurring 4 AM sweep
/// (<c>ExpireDueRequestedBookingsRecurringJob</c>) catches any bookings that miss this
/// schedule (e.g., requests made before the Hangfire server was up).
/// </summary>
internal sealed class HangfireBookingExpiryScheduler(IBackgroundJobClient jobs) : IBookingExpiryScheduler
{
    public string Schedule(Guid bookingId, DateTime atUtc)
    {
        if (atUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("atUtc must be UTC.", nameof(atUtc));

        // BackgroundJob.Schedule resolves the IMediator via Hangfire's job activator at fire
        // time, so the request runs in a fresh DI scope (fresh DbContext, fresh handlers).
        return jobs.Schedule<IMediator>(
            m => m.Send(new ExpireRequestedBookingCommand(bookingId), CancellationToken.None),
            atUtc - DateTime.UtcNow);
    }
}

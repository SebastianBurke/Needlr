using Hangfire;
using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.MessageThreads.LockMessageThread;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Schedules the 90-day post-terminal-state thread lock via Hangfire's one-off API. The
/// safety-net recurring sweep <c>LockOverdueThreadsRecurringJob</c> catches threads whose
/// schedule was missed (e.g., booking moved to terminal state before this scheduler was
/// wired, or the Hangfire server was down during the window).
/// </summary>
internal sealed class HangfireThreadLockScheduler(IBackgroundJobClient jobs) : IThreadLockScheduler
{
    public string Schedule(Guid bookingId, DateTime atUtc)
    {
        if (atUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("atUtc must be UTC.", nameof(atUtc));

        return jobs.Schedule<IMediator>(
            m => m.Send(new LockMessageThreadCommand(bookingId), CancellationToken.None),
            atUtc - DateTime.UtcNow);
    }
}

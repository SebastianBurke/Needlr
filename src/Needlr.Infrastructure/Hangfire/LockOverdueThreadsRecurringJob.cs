using MediatR;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.MessageThreads.LockMessageThread;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Hangfire-friendly recurring job that locks any thread whose related booking reached a
/// terminal state &gt; 90 days ago. Catches threads that escaped the per-booking schedule.
/// Phase 12 ships the class; Phase 14 wires <c>RecurringJob.AddOrUpdate</c> + cron.
/// </summary>
public sealed class LockOverdueThreadsRecurringJob(
    IMessageThreadRepository threads,
    IMediator mediator,
    IClock clock,
    ILogger<LockOverdueThreadsRecurringJob> logger)
{
    public const string JobId = "lock-overdue-message-threads";
    public const int LockAfterDays = 90;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = clock.UtcNow.AddDays(-LockAfterDays);
        var due = await threads.ListLockableAsync(cutoff, cancellationToken);

        var failures = 0;
        foreach (var thread in due)
        {
            try
            {
                var result = await mediator.Send(
                    new LockMessageThreadCommand(thread.BookingId), cancellationToken);
                if (result.IsFailure)
                {
                    failures++;
                    logger.LogWarning("Failed to lock thread {ThreadId}: {Error}",
                        thread.Id, result.FirstError?.Message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Failed to lock thread {ThreadId}", thread.Id);
            }
        }

        logger.LogInformation(
            "Locked {Total} overdue thread(s); {Failures} failure(s).",
            due.Count - failures, failures);
    }
}

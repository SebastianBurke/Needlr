namespace Needlr.Application.Abstractions;

/// <summary>
/// Schedules a one-off Hangfire job that locks the message thread for a booking 90 days
/// after the booking reaches a terminal state (FEATURE_SPECS.md § Booking lifecycle
/// post-confirmation, ADR-003 § Retention). Tests substitute a no-op so they don't require
/// a Hangfire server; the recurring sweep <c>LockOverdueThreadsRecurringJob</c> is the
/// safety net for missed schedules.
/// </summary>
public interface IThreadLockScheduler
{
    /// <summary>
    /// Schedules <c>LockMessageThreadCommand</c> for the thread of <paramref name="bookingId"/>
    /// at <paramref name="atUtc"/>. Returns the scheduled job's id (opaque to callers).
    /// </summary>
    string Schedule(Guid bookingId, DateTime atUtc);
}

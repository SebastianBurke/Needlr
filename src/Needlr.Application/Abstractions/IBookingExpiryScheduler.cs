namespace Needlr.Application.Abstractions;

/// <summary>
/// Schedules the per-booking 7-day auto-expire job (FEATURE_SPECS.md § Artist response
/// options). Invocation lives in Infrastructure where Hangfire's static API can be reached.
/// In tests this is replaced with a no-op so we don't depend on a Hangfire server.
/// </summary>
public interface IBookingExpiryScheduler
{
    /// <summary>
    /// Schedules <c>ExpireRequestedBookingCommand</c> for <paramref name="bookingId"/> to
    /// fire at <paramref name="atUtc"/>. Returns the scheduled job's id (opaque to callers).
    /// </summary>
    string Schedule(Guid bookingId, DateTime atUtc);
}

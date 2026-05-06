using Needlr.Application.Abstractions;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Test-time replacement for <see cref="IBookingExpiryScheduler"/>. Hangfire is not running
/// in tests; we don't want booking creation to enqueue real jobs against the test schema.
/// Tests that need to exercise the expiry path call <c>ExpireRequestedBookingCommand</c>
/// directly (or the recurring-job class) rather than rely on a scheduled trigger.
/// </summary>
internal sealed class NoopBookingExpiryScheduler : IBookingExpiryScheduler
{
    public string Schedule(Guid bookingId, DateTime atUtc) =>
        $"noop-{bookingId:N}";
}

using Needlr.Application.Abstractions;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Test-time replacement for <see cref="IThreadLockScheduler"/>. Hangfire isn't running in
/// tests; we don't want booking-completion to enqueue real jobs against the test schema.
/// Tests that need to exercise the lock path call <c>LockMessageThreadCommand</c> directly.
/// </summary>
internal sealed class NoopThreadLockScheduler : IThreadLockScheduler
{
    public string Schedule(Guid bookingId, DateTime atUtc) =>
        $"noop-thread-lock-{bookingId:N}";
}

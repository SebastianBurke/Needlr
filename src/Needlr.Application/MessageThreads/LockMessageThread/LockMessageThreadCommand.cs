using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.LockMessageThread;

/// <summary>
/// Locks a thread (no further messages may be sent; existing messages remain visible to
/// the parties). Idempotent: a thread already in <c>Locked</c> state resolves cleanly.
/// Fired by the per-booking Hangfire schedule and by the nightly safety-net sweep.
/// </summary>
public sealed record LockMessageThreadCommand(Guid BookingId) : ICommand;

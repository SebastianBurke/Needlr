using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.SendMessage;

/// <summary>
/// Sends a message in a booking-scoped thread. Per ADR-003 / FEATURE_SPECS § Gating, the
/// thread must be Active (i.e., DepositCaptured) and the caller must be one of the two
/// parties (booking customer or booking artist). No content stripping post-acceptance —
/// adults can exchange logistics for the day-of session.
/// </summary>
public sealed record SendMessageCommand(Guid ThreadId, string Body) : ICommand<Guid>;

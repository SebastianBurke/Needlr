using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.MarkMessageRead;

/// <summary>
/// Marks a message read by the calling user. Idempotent: re-marking a message you already
/// read leaves <c>ReadAt</c> at its existing value.
/// </summary>
public sealed record MarkMessageReadCommand(Guid MessageId) : ICommand;

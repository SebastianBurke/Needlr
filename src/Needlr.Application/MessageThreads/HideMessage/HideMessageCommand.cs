using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.HideMessage;

/// <summary>
/// Admin replaces a message body with a redaction notice in response to a report or audit.
/// Per ADR-003, message text is retained indefinitely with admin-only access — "hide" is
/// soft and reversible by querying audit history; we don't actually drop the row. The
/// public detail call returns the redacted body to the parties.
/// </summary>
public sealed record HideMessageCommand(Guid MessageId, string Reason) : ICommand;

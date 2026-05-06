using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.ReportMessage;

/// <summary>
/// Either party reports a message in their thread (FEATURE_SPECS § Moderation). Sets
/// <c>IsReportedFlag</c> so the FE can dim the message in-thread; admin queue surfaces it
/// for review. A user reporting their own message is allowed (e.g., flagging an attachment
/// that auto-attached the wrong file).
/// </summary>
public sealed record ReportMessageCommand(
    Guid MessageId,
    MessageReportReason Reason,
    string? Note) : ICommand<Guid>;

using Needlr.Domain.Enums;

namespace Needlr.Domain.Messaging;

/// <summary>
/// A user's report of a message. Routed to the admin queue; admin actions are recorded via
/// the resolution fields.
/// </summary>
public sealed class MessageReport
{
    public const int NoteMaxLength = 2000;

    public Guid Id { get; init; }
    public Guid MessageId { get; init; }
    public Guid ReportedByUserId { get; init; }
    public MessageReportReason Reason { get; init; }
    public string? Note { get; set; }
    public DateTime ReportedAt { get; init; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByAdminId { get; set; }
    public MessageReportResolution? Resolution { get; set; }

    private MessageReport() { }

    public MessageReport(
        Guid id,
        Guid messageId,
        Guid reportedByUserId,
        MessageReportReason reason,
        DateTime reportedAt,
        string? note = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (messageId == Guid.Empty) throw new ArgumentException("MessageId is required.", nameof(messageId));
        if (reportedByUserId == Guid.Empty)
            throw new ArgumentException("ReportedByUserId is required.", nameof(reportedByUserId));
        if (note is { Length: > NoteMaxLength })
            throw new ArgumentException($"Note must be <= {NoteMaxLength} chars.", nameof(note));
        if (reportedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ReportedAt must be UTC.", nameof(reportedAt));

        Id = id;
        MessageId = messageId;
        ReportedByUserId = reportedByUserId;
        Reason = reason;
        Note = note?.Trim();
        ReportedAt = reportedAt;
    }
}

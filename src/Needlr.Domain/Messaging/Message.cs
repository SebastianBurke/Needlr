using Needlr.Domain.Bookings;

namespace Needlr.Domain.Messaging;

/// <summary>
/// One message in a booking-scoped <see cref="MessageThread"/>. Per ADR-003 messages cannot be
/// edited or deleted by users (admin can soft-hide via reports). Text bodies are retained
/// indefinitely with admin-only access after the thread locks; only attachment blobs purge.
/// </summary>
public sealed class Message
{
    public const int BodyMaxLength = 5000;

    public Guid Id { get; init; }
    public Guid ThreadId { get; init; }
    public Guid SenderId { get; init; }
    public string Body { get; set; } = null!;
    public DateTime SentAt { get; init; }
    public DateTime? ReadAt { get; set; }
    public bool IsReportedFlag { get; set; }

    public ICollection<BookingAttachment> Attachments { get; set; } = new List<BookingAttachment>();

    private Message() { }

    public Message(Guid id, Guid threadId, Guid senderId, string body, DateTime sentAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (threadId == Guid.Empty) throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (senderId == Guid.Empty) throw new ArgumentException("SenderId is required.", nameof(senderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (body.Length > BodyMaxLength)
            throw new ArgumentException($"Body must be <= {BodyMaxLength} chars.", nameof(body));
        if (sentAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("SentAt must be UTC.", nameof(sentAt));

        Id = id;
        ThreadId = threadId;
        SenderId = senderId;
        Body = body;
        SentAt = sentAt;
    }
}

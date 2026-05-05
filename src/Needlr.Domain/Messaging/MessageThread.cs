using Needlr.Domain.Enums;

namespace Needlr.Domain.Messaging;

/// <summary>
/// One-to-one with a booking. Opens at <see cref="Enums.BookingStatus.DepositCaptured"/>; transitions
/// to <see cref="MessageThreadStatus.Locked"/> 90 days after the booking reaches a terminal state
/// (ADR-003 § Privacy / FEATURE_SPECS.md § Booking lifecycle post-confirmation).
/// </summary>
public sealed class MessageThread
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? LockedAt { get; set; }
    public MessageThreadStatus Status { get; set; } = MessageThreadStatus.Active;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    private MessageThread() { }

    public MessageThread(Guid id, Guid bookingId, DateTime openedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (openedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("OpenedAt must be UTC.", nameof(openedAt));

        Id = id;
        BookingId = bookingId;
        OpenedAt = openedAt;
    }
}

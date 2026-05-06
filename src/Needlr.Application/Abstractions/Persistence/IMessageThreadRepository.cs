using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Messaging;

namespace Needlr.Application.Abstractions.Persistence;

public interface IMessageThreadRepository
{
    Task<MessageThread?> GetByIdAsync(Guid threadId, CancellationToken cancellationToken = default);

    Task<MessageThread?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Joins onto the booking so the caller can do the artist/customer-party check without
    /// a second round-trip. Returns the thread + booking, or null if either is missing.
    /// </summary>
    Task<(MessageThread Thread, Booking Booking)?> GetWithBookingAsync(
        Guid threadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active threads in which <paramref name="userId"/> is either the booking's customer
    /// or the booking's artist (resolved via the artist's UserId). Sorted by latest message
    /// time desc.
    /// </summary>
    Task<PagedResult<MessageThread>> ListActiveForUserAsync(
        Guid userId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Threads still <c>Active</c> whose related booking reached terminal state &gt;= cutoff days ago.</summary>
    Task<IReadOnlyList<MessageThread>> ListLockableAsync(
        DateTime cutoffUtc, CancellationToken cancellationToken = default);

    void Add(MessageThread thread);
}

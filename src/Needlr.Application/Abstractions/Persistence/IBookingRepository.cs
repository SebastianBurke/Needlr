using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions.Persistence;

/// <summary>
/// Booking entity paired with the two display names a UI surface always wants alongside it
/// (the customer's profile name and the artist's stage name) and the unread-message count
/// for the requesting user (zero when no thread exists or the thread is locked). Both
/// customer and artist have the operational right to identify each other from Requested
/// onward — the customer needs to know which artist they booked with; the artist needs to
/// recognize the customer to triage the request before accepting (and decline a problematic
/// booker safely).
/// </summary>
public sealed record BookingWithNames(
    Booking Booking,
    string CustomerDisplayName,
    string ArtistDisplayName,
    int UnreadMessageCount);

/// <summary>
/// Phase 7 introduced this for the healed-photo flow; Phase 9 extended it with the
/// projector/iCal lookups; Phase 10 finishes the booking-lifecycle surface.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Returns the booking only if it is owned by the supplied customer.</summary>
    Task<Booking?> GetByIdForCustomerAsync(Guid bookingId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Returns the booking only if it belongs to the supplied artist.</summary>
    Task<Booking?> GetByIdForArtistAsync(Guid bookingId, Guid artistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Booking + counterparty display names projection. Used by detail/list query handlers
    /// to render "you booked with X" and "request from Y" without a second round-trip.
    /// Detail surface doesn't need an unread-count (the embedded thread marks read on view),
    /// so this overload always returns <c>UnreadMessageCount = 0</c>.
    /// </summary>
    Task<BookingWithNames?> GetByIdWithNamesAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated bookings for a given customer, optionally filtered to a specific status.
    /// Sorted newest-requested-first.
    /// </summary>
    Task<PagedResult<Booking>> ListForCustomerAsync(
        Guid customerId, BookingStatus? status, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated bookings for a given artist, optionally filtered to a specific status.
    /// Sorted newest-requested-first.
    /// </summary>
    Task<PagedResult<Booking>> ListForArtistAsync(
        Guid artistId, BookingStatus? status, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="ListForCustomerAsync"/> but each row carries both display names plus
    /// the unread-message count for <paramref name="requestingUserId"/>. The unread count uses
    /// the same predicate as <see cref="IMessageRepository.CountUnreadForUserAsync"/>: only
    /// Active threads, only messages where <c>SenderId != requestingUserId</c>, and where
    /// <c>ReadAt IS NULL</c>.
    /// </summary>
    Task<PagedResult<BookingWithNames>> ListForCustomerWithNamesAsync(
        Guid customerId, Guid requestingUserId, BookingStatus? status,
        PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="ListForArtistAsync"/> but each row carries both display names plus
    /// the unread-message count for <paramref name="requestingUserId"/>.
    /// </summary>
    Task<PagedResult<BookingWithNames>> ListForArtistWithNamesAsync(
        Guid artistId, Guid requestingUserId, BookingStatus? status,
        PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bookings in <see cref="BookingStatus.Requested"/> whose <c>RequestedAt</c> is on or
    /// before <paramref name="cutoffUtc"/>. Used by the 7-day expiry job.
    /// </summary>
    Task<IReadOnlyList<Booking>> ListRequestedExpiredAsync(
        DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bookings for the artist whose <c>ConfirmedSessionDate</c>'s date falls in [from, to]
    /// and whose status consumes capacity in the projector (Accepted/DepositCaptured/Confirmed/InProgress).
    /// Used by <c>IAvailabilityProjector</c> to subtract booked hours from a day's capacity.
    /// </summary>
    Task<IReadOnlyList<Booking>> ListConsumingForArtistInWindowAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bookings for the artist that should appear on the iCal feed: any booking with a
    /// non-null <c>ConfirmedSessionDate</c> on or after <paramref name="from"/> whose status
    /// has not been cancelled/declined/expired (Accepted/DepositCaptured/Confirmed/InProgress/Completed).
    /// </summary>
    Task<IReadOnlyList<Booking>> ListConfirmedForArtistFromAsync(
        Guid artistId, DateTime from, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}

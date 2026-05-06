using Needlr.Domain.Bookings;

namespace Needlr.Application.Abstractions.Persistence;

/// <summary>
/// Phase 7 introduced this for the healed-photo flow; Phase 9 extended it with the
/// projector/iCal lookups. Phase 10 will extend it with the full booking lifecycle.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Returns the booking only if it is owned by the supplied customer.</summary>
    Task<Booking?> GetByIdForCustomerAsync(Guid bookingId, Guid customerId, CancellationToken cancellationToken = default);

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

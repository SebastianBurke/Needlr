using Needlr.Domain.Bookings;

namespace Needlr.Application.Abstractions.Persistence;

/// <summary>
/// Phase 7 needs only enough booking access to run the healed-photo flow. Phase 10
/// extends this interface with the full booking lifecycle.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Returns the booking only if it is owned by the supplied customer.</summary>
    Task<Booking?> GetByIdForCustomerAsync(Guid bookingId, Guid customerId, CancellationToken cancellationToken = default);

    void Add(Booking booking);
}

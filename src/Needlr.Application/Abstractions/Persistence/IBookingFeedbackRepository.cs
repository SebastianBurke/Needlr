using Needlr.Domain.Bookings;

namespace Needlr.Application.Abstractions.Persistence;

public interface IBookingFeedbackRepository
{
    Task<BookingFeedback?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingFeedback>> ListRecentForArtistAsync(
        Guid artistId, int take, CancellationToken cancellationToken = default);

    void Add(BookingFeedback feedback);
}

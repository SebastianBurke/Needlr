using Needlr.Domain.Availability;

namespace Needlr.Application.Abstractions.Persistence;

public interface IBookingWindowRepository
{
    Task<BookingWindow?> GetByIdAsync(Guid windowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingWindow>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default);

    void Add(BookingWindow window);
    void Remove(BookingWindow window);
}

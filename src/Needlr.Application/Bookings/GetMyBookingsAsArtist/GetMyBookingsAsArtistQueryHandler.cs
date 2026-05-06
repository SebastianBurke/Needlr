using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Bookings.GetMyBookingsAsArtist;

internal sealed class GetMyBookingsAsArtistQueryHandler(
    ICurrentUser currentUser,
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings)
    : IRequestHandler<GetMyBookingsAsArtistQuery, Result<PagedResult<BookingSummaryDto>>>
{
    public async Task<Result<PagedResult<BookingSummaryDto>>> Handle(
        GetMyBookingsAsArtistQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<PagedResult<BookingSummaryDto>>.Failure(
                Error.Forbidden("This list is for artists only."));

        // Messages are keyed by auth user id (Message.SenderId), not artist id, so the unread
        // count needs the artist's underlying user id — fetched via ICurrentUser.
        var requestingUserId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated artist must have a UserId claim.");

        var page = await bookings.ListForArtistWithNamesAsync(
            artistId.Value,
            requestingUserId,
            request.Status,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);

        var items = page.Items
            .Select(r => new BookingSummaryDto(
                r.Booking.Id, r.Booking.CustomerId, r.CustomerDisplayName,
                r.Booking.ArtistId, r.ArtistDisplayName,
                r.Booking.BookingType, r.Booking.Status,
                r.Booking.RequestedAt, r.Booking.RequestedDate, r.Booking.ConfirmedSessionDate,
                r.UnreadMessageCount))
            .ToList();
        return Result<PagedResult<BookingSummaryDto>>.Success(
            new PagedResult<BookingSummaryDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}

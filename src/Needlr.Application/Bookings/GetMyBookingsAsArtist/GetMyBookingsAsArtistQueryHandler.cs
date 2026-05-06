using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Bookings.GetMyBookingsAsArtist;

internal sealed class GetMyBookingsAsArtistQueryHandler(
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

        var page = await bookings.ListForArtistAsync(
            artistId.Value,
            request.Status,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);

        var items = page.Items
            .Select(b => new BookingSummaryDto(
                b.Id, b.CustomerId, b.ArtistId, b.BookingType, b.Status,
                b.RequestedAt, b.RequestedDate, b.ConfirmedSessionDate))
            .ToList();
        return Result<PagedResult<BookingSummaryDto>>.Success(
            new PagedResult<BookingSummaryDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}

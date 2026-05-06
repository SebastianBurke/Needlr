using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.GetMyBookingsAsCustomer;

internal sealed class GetMyBookingsAsCustomerQueryHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings)
    : IRequestHandler<GetMyBookingsAsCustomerQuery, Result<PagedResult<BookingSummaryDto>>>
{
    public async Task<Result<PagedResult<BookingSummaryDto>>> Handle(
        GetMyBookingsAsCustomerQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Customer))
            return Result<PagedResult<BookingSummaryDto>>.Failure(
                Error.Forbidden("This list is for customer users only."));

        var customerId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated customer must have a UserId claim.");

        var page = await bookings.ListForCustomerWithNamesAsync(
            customerId,
            customerId,
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

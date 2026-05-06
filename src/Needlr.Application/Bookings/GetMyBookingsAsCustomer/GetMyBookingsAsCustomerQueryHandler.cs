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

        var page = await bookings.ListForCustomerAsync(
            customerId,
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

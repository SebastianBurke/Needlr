using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.GetMyBookingsAsCustomer;

public sealed record GetMyBookingsAsCustomerQuery(
    BookingStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<BookingSummaryDto>>;

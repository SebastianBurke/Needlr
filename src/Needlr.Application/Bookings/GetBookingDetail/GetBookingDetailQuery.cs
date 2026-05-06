using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.GetBookingDetail;

public sealed record GetBookingDetailQuery(Guid BookingId) : IQuery<BookingDetailDto>;

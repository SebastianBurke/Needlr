using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.ExpireRequestedBooking;

internal sealed class ExpireRequestedBookingCommandHandler(
    IBookingRepository bookings) : IRequestHandler<ExpireRequestedBookingCommand, Result>
{
    public async Task<Result> Handle(ExpireRequestedBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        // Idempotent: any non-Requested booking resolves cleanly with no change. The job
        // can re-fire on a partially-processed batch without surfacing errors.
        if (booking.Status != BookingStatus.Requested)
            return Result.Success();

        booking.Status = BookingStatus.Expired;
        // Pre-auth void deferred to Phase 11 (Stripe). Pre-auth holds expire on Stripe's
        // side after 7 days anyway; the explicit void in Phase 11 just tidies up early.
        return Result.Success();
    }
}

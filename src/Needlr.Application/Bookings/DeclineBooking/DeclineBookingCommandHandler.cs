using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.DeclineBooking;

internal sealed class DeclineBookingCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings) : IRequestHandler<DeclineBookingCommand, Result>
{
    public async Task<Result> Handle(DeclineBookingCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can decline bookings."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        if (booking.Status != BookingStatus.Requested)
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot decline a booking in status {booking.Status}."));

        booking.Status = BookingStatus.Declined;
        booking.DeclineReason = request.Reason;
        booking.DeclineNote = request.Note?.Trim();
        // Pre-auth void deferred to Phase 11. The Stripe webhook will eventually flip a
        // sub-state when implemented; for now nothing else to do.
        return Result.Success();
    }
}

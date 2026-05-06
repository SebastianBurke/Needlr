using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.MarkBookingInProgress;

internal sealed class MarkBookingInProgressCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings) : IRequestHandler<MarkBookingInProgressCommand, Result>
{
    public async Task<Result> Handle(MarkBookingInProgressCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can mark bookings in-progress."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        // Phase 10: Stripe capture isn't wired yet, so accept either Accepted or the
        // post-capture states (DepositCaptured/Confirmed). After Phase 11 the artist UI will
        // typically only see Confirmed bookings reach this command.
        if (booking.Status is not (BookingStatus.Accepted or BookingStatus.DepositCaptured or BookingStatus.Confirmed))
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot start a booking in status {booking.Status}."));

        booking.Status = BookingStatus.InProgress;
        return Result.Success();
    }
}

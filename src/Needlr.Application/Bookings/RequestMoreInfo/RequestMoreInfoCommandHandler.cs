using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RequestMoreInfo;

internal sealed class RequestMoreInfoCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings) : IRequestHandler<RequestMoreInfoCommand, Result>
{
    public async Task<Result> Handle(RequestMoreInfoCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can request more info."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        if (booking.Status != BookingStatus.Requested)
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot request info on a booking in status {booking.Status}."));

        booking.Status = BookingStatus.AwaitingCustomerInfo;
        return Result.Success();
    }
}

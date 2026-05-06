using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.AcceptBooking;

internal sealed class AcceptBookingCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<AcceptBookingCommand, Result>
{
    public async Task<Result> Handle(AcceptBookingCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can accept bookings."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        if (booking.Status != BookingStatus.Requested)
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot accept a booking in status {booking.Status}."));

        booking.Status = BookingStatus.Accepted;
        booking.AcceptedAt = clock.UtcNow;
        booking.ConfirmedSessionDate = request.ConfirmedSessionDateUtc;

        // Flush before the projector so it sees the now-consuming booking.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result.Success();
    }
}

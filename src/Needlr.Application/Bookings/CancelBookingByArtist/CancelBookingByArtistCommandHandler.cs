using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Bookings.CancelBookingByCustomer;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.CancelBookingByArtist;

internal sealed class CancelBookingByArtistCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelBookingByArtistCommand, Result<CancelBookingResult>>
{
    public async Task<Result<CancelBookingResult>> Handle(
        CancelBookingByArtistCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<CancelBookingResult>.Failure(
                Error.Forbidden("Only artists can cancel via this endpoint."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result<CancelBookingResult>.Failure(Error.NotFound("Booking"));

        if (!IsCancellableByArtist(booking.Status))
            return Result<CancelBookingResult>.Failure(Error.FailedPrecondition(
                $"Cannot cancel a booking in status {booking.Status}."));

        // Per FEATURE_SPECS § Deposit handling, artist-side cancellation is full refund
        // regardless of policy or timing.
        var refund = CancellationRefundPolicy.ArtistCancellationRefund(booking.DepositAmountCad);

        var wasConsumingCapacity = IsCapacityConsuming(booking.Status);
        booking.Status = BookingStatus.CancelledByArtist;

        if (wasConsumingCapacity)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        }

        return Result<CancelBookingResult>.Success(new CancelBookingResult(refund));
    }

    private static bool IsCancellableByArtist(BookingStatus status) => status is
        BookingStatus.Requested or BookingStatus.AwaitingCustomerInfo or
        BookingStatus.Accepted or BookingStatus.DepositCaptured or BookingStatus.Confirmed;

    private static bool IsCapacityConsuming(BookingStatus status) => status is
        BookingStatus.Accepted or BookingStatus.DepositCaptured or
        BookingStatus.Confirmed or BookingStatus.InProgress;
}

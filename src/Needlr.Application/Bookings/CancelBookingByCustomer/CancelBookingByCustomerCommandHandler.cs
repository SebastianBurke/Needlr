using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.CancelBookingByCustomer;

internal sealed class CancelBookingByCustomerCommandHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CancelBookingByCustomerCommand, Result<CancelBookingResult>>
{
    public async Task<Result<CancelBookingResult>> Handle(
        CancelBookingByCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Customer))
            return Result<CancelBookingResult>.Failure(
                Error.Forbidden("Only customers can cancel via this endpoint."));

        var customerId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated customer must have a UserId claim.");

        var booking = await bookings.GetByIdForCustomerAsync(request.BookingId, customerId, cancellationToken);
        if (booking is null)
            return Result<CancelBookingResult>.Failure(Error.NotFound("Booking"));

        if (!IsCancellableByCustomer(booking.Status))
            return Result<CancelBookingResult>.Failure(Error.FailedPrecondition(
                $"Cannot cancel a booking in status {booking.Status}."));

        var refund = CancellationRefundPolicy.CustomerCancellationRefund(
            booking.CancellationPolicySnapshot,
            booking.DepositAmountCad,
            booking.ConfirmedSessionDate,
            clock.UtcNow);

        var wasConsumingCapacity = IsCapacityConsuming(booking.Status);
        booking.Status = BookingStatus.CancelledByCustomer;

        if (wasConsumingCapacity)
        {
            // Capacity that was held for the now-cancelled session goes back to the projection.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await projector.RebuildRollingWindowAsync(booking.ArtistId, cancellationToken);
        }

        return Result<CancelBookingResult>.Success(new CancelBookingResult(refund));
    }

    private static bool IsCancellableByCustomer(BookingStatus status) => status is
        BookingStatus.Requested or BookingStatus.AwaitingCustomerInfo or
        BookingStatus.Accepted or BookingStatus.DepositCaptured or BookingStatus.Confirmed;

    private static bool IsCapacityConsuming(BookingStatus status) => status is
        BookingStatus.Accepted or BookingStatus.DepositCaptured or
        BookingStatus.Confirmed or BookingStatus.InProgress;
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RespondWithMoreInfo;

internal sealed class RespondWithMoreInfoCommandHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IContactInfoStripper stripper) : IRequestHandler<RespondWithMoreInfoCommand, Result>
{
    public async Task<Result> Handle(RespondWithMoreInfoCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Customer))
            return Result.Failure(Error.Forbidden("Only customers can respond with more info."));

        var customerId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated customer must have a UserId claim.");

        var booking = await bookings.GetByIdForCustomerAsync(request.BookingId, customerId, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        if (booking.Status != BookingStatus.AwaitingCustomerInfo)
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot respond on a booking in status {booking.Status}."));

        booking.Description = stripper.Strip(request.Description);
        booking.RequestedDate = request.RequestedDate;
        booking.EstimatedDurationHours = request.EstimatedDurationHours;
        booking.BodyPlacement = request.BodyPlacement;
        booking.ApproximateSizeCm = request.ApproximateSizeCm;
        booking.EstimatedTotalCad = request.EstimatedTotalCad;
        booking.Status = BookingStatus.Requested;
        return Result.Success();
    }
}

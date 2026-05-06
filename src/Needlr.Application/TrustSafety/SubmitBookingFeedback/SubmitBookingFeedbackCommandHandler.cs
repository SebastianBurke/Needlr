using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;

namespace Needlr.Application.TrustSafety.SubmitBookingFeedback;

internal sealed class SubmitBookingFeedbackCommandHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IBookingFeedbackRepository feedbacks,
    IClock clock) : IRequestHandler<SubmitBookingFeedbackCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitBookingFeedbackCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Customer) || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only customers can submit feedback."));
        var customerId = currentUser.UserId.Value;

        var booking = await bookings.GetByIdForCustomerAsync(request.BookingId, customerId, cancellationToken);
        if (booking is null)
            return Result<Guid>.Failure(Error.NotFound("Booking"));
        if (booking.Status != BookingStatus.Completed)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "Feedback is only accepted against Completed bookings."));

        var existing = await feedbacks.GetByBookingIdAsync(booking.Id, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(Error.Conflict("Feedback has already been submitted for this booking."));

        var feedback = new BookingFeedback(
            id: Guid.NewGuid(),
            bookingId: booking.Id,
            customerId: customerId,
            communicationRating: request.CommunicationRating,
            cleanlinessRating: request.CleanlinessRating,
            respectedDesignBriefRating: request.RespectedDesignBriefRating,
            wouldBookAgain: request.WouldBookAgain,
            submittedAt: clock.UtcNow,
            freeText: request.FreeText);
        feedbacks.Add(feedback);
        return Result<Guid>.Success(feedback.Id);
    }
}

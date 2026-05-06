using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.DeclineBooking;

internal sealed class DeclineBookingCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists,
    IBookingRepository bookings,
    IStripeService stripe,
    INotificationDispatcher notifications) : IRequestHandler<DeclineBookingCommand, Result>
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

        // Void the pre-auth so the customer's hold is released. Per ADR-005 the cancel
        // call goes against the artist's connected account.
        if (!string.IsNullOrEmpty(booking.StripePaymentIntentId))
        {
            var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
            if (artist?.StripeConnectAccountId is { } account)
            {
                await stripe.CancelPaymentIntentAsync(booking.StripePaymentIntentId!, account, cancellationToken);
            }
        }

        // Tell the customer their request was declined (FEATURE_SPECS § Notifications).
        var pushReason = request.Reason.ToString();
        await notifications.DispatchAsync(
            booking.CustomerId,
            NotificationType.BookingDeclined,
            new NotificationContent(
                EmailSubject: "Your booking request was declined",
                EmailBody: $"The artist declined your request. Reason: {pushReason}. The pre-authorization on your card has been released.",
                PushTitle: "Booking declined",
                PushBody: pushReason),
            cancellationToken);

        return Result.Success();
    }
}

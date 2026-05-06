using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.ExpireRequestedBooking;

internal sealed class ExpireRequestedBookingCommandHandler(
    IArtistRepository artists,
    IBookingRepository bookings,
    IStripeService stripe,
    INotificationDispatcher notifications) : IRequestHandler<ExpireRequestedBookingCommand, Result>
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

        // Void the pre-auth proactively. Stripe's own pre-auth holds expire on the bank's
        // schedule (~7 days) but cancelling explicitly returns the funds sooner.
        if (!string.IsNullOrEmpty(booking.StripePaymentIntentId))
        {
            var artist = await artists.GetByIdAsync(booking.ArtistId, cancellationToken);
            if (artist?.StripeConnectAccountId is { } account)
            {
                await stripe.CancelPaymentIntentAsync(booking.StripePaymentIntentId!, account, cancellationToken);
            }
        }

        await notifications.DispatchAsync(
            booking.CustomerId,
            NotificationType.BookingExpired,
            new NotificationContent(
                EmailSubject: "Your booking request expired",
                EmailBody: "The artist didn't respond within 7 days; your pre-authorization has been released. Feel free to try another artist.",
                PushTitle: "Booking expired",
                PushBody: "No response in 7 days"),
            cancellationToken);

        return Result.Success();
    }
}

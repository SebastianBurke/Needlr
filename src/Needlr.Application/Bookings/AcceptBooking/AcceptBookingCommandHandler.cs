using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.AcceptBooking;

internal sealed class AcceptBookingCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists,
    IBookingRepository bookings,
    IStripeService stripe,
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

        // Capture the pre-authorized deposit on the artist's connected account. The
        // payment_intent.succeeded webhook flips status Accepted → DepositCaptured →
        // Confirmed (FEATURE_SPECS § Artist response options) when Stripe acknowledges.
        // We don't pre-empt that here; the webhook is the single source of truth so the
        // post-capture states reflect actual movement of money.
        if (!string.IsNullOrEmpty(booking.StripePaymentIntentId))
        {
            var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
            if (artist?.StripeConnectAccountId is { } account)
            {
                await stripe.CapturePaymentIntentAsync(booking.StripePaymentIntentId!, account, cancellationToken);
            }
        }

        // Flush before the projector so it sees the now-consuming booking.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result.Success();
    }
}

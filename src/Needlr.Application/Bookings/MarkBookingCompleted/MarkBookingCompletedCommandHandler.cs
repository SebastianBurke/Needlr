using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.MarkBookingCompleted;

internal sealed class MarkBookingCompletedCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<MarkBookingCompletedCommand, Result>
{
    public async Task<Result> Handle(MarkBookingCompletedCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can mark bookings complete."));

        var booking = await bookings.GetByIdForArtistAsync(request.BookingId, artistId.Value, cancellationToken);
        if (booking is null)
            return Result.Failure(Error.NotFound("Booking"));

        // Allow Completion from any post-acceptance state. Marking InProgress is optional
        // (FEATURE_SPECS.md § Booking lifecycle post-confirmation), so artists who skip it
        // can complete directly from Accepted/DepositCaptured/Confirmed.
        if (booking.Status is not (BookingStatus.Accepted or BookingStatus.DepositCaptured
            or BookingStatus.Confirmed or BookingStatus.InProgress))
            return Result.Failure(Error.FailedPrecondition(
                $"Cannot complete a booking in status {booking.Status}."));

        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = clock.UtcNow;

        // Free up forward capacity in case the artist re-uses the same date for a different
        // engagement; spec calls out that completion triggers projector recompute.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result.Success();
    }
}

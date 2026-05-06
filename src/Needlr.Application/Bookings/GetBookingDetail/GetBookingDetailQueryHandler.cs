using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.GetBookingDetail;

internal sealed class GetBookingDetailQueryHandler(
    ICurrentUser currentUser,
    IStudioAuthorization studioAuthorization,
    IBookingRepository bookings) : IRequestHandler<GetBookingDetailQuery, Result<BookingDetailDto>>
{
    public async Task<Result<BookingDetailDto>> Handle(
        GetBookingDetailQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Result<BookingDetailDto>.Failure(Error.Unauthorized());

        var row = await bookings.GetByIdWithNamesAsync(request.BookingId, cancellationToken);
        if (row is null)
            return Result<BookingDetailDto>.Failure(Error.NotFound("Booking"));
        var booking = row.Booking;

        // Visibility rule: customer-on-the-booking, artist-on-the-booking, or admin.
        var userId = currentUser.UserId;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);
        var isCustomerParty = userId == booking.CustomerId;
        var isArtistParty = false;
        if (currentUser.IsInRole(UserRole.Artist))
        {
            var callerArtistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
            isArtistParty = callerArtistId == booking.ArtistId;
        }

        if (!(isAdmin || isCustomerParty || isArtistParty))
            return Result<BookingDetailDto>.Failure(Error.Forbidden("Not a party to this booking."));

        return Result<BookingDetailDto>.Success(new BookingDetailDto(
            booking.Id,
            booking.CustomerId,
            row.CustomerDisplayName,
            booking.ArtistId,
            row.ArtistDisplayName,
            booking.StudioId,
            booking.BookingType,
            booking.Status,
            booking.RequestedAt,
            booking.RequestedDate,
            booking.EstimatedDurationHours,
            booking.Description,
            booking.BodyPlacement,
            booking.ApproximateSizeCm,
            booking.EstimatedTotalCad,
            booking.DepositAmountCad,
            booking.AcceptedAt,
            booking.ConfirmedSessionDate,
            booking.CompletedAt,
            booking.DepositCapturedAt,
            booking.CancellationPolicySnapshot,
            booking.DeclineReason,
            booking.DeclineNote));
    }
}

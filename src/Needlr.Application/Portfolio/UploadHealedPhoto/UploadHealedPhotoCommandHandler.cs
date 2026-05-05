using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.UploadHealedPhoto;

internal sealed class UploadHealedPhotoCommandHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IPortfolioPieceRepository pieces,
    ISessionPhotoRepository photos,
    IImageStorage imageStorage,
    IClock clock) : IRequestHandler<UploadHealedPhotoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadHealedPhotoCommand request, CancellationToken cancellationToken)
    {
        var customerId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated customer must have a UserId claim.");

        var booking = await bookings.GetByIdForCustomerAsync(request.BookingId, customerId, cancellationToken);
        if (booking is null)
            return Result<Guid>.Failure(Error.NotFound("Booking"));

        if (booking.Status != BookingStatus.Completed)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "Healed photos can only be uploaded for Completed bookings."));

        var piece = await pieces.FindByLinkedBookingAsync(booking.Id, cancellationToken);
        if (piece is null)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "The artist hasn't created the portfolio piece for this booking yet."));

        var imageKey = await imageStorage.UploadAsync(
            request.FileContent, request.ContentType,
            keyPrefix: $"healed/{piece.Id:N}", cancellationToken);

        // Append after any existing photos. Order = current count.
        var nextOrder = piece.Sessions.Count;
        var photo = new SessionPhoto(
            id: Guid.NewGuid(),
            portfolioPieceId: piece.Id,
            order: nextOrder,
            photoType: PhotoType.Healed,
            imageUrl: imageKey,
            uploadedByUserId: customerId,
            uploadedByRole: UploadedByRole.Customer,
            uploadedAt: clock.UtcNow,
            linkedSessionDate: booking.CompletedAt);
        photos.Add(photo);

        return Result<Guid>.Success(photo.Id);
    }
}

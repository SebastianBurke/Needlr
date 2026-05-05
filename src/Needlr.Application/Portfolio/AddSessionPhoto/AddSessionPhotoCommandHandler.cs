using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.AddSessionPhoto;

internal sealed class AddSessionPhotoCommandHandler(
    IStudioAuthorization studioAuthorization,
    ICurrentUser currentUser,
    IPortfolioPieceRepository pieces,
    ISessionPhotoRepository photos,
    IImageStorage imageStorage,
    IClock clock) : IRequestHandler<AddSessionPhotoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddSessionPhotoCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can add session photos."));

        var piece = await pieces.GetByIdAsync(request.PortfolioPieceId, cancellationToken);
        if (piece is null)
            return Result<Guid>.Failure(Error.NotFound("PortfolioPiece"));

        if (piece.ArtistId != artistId.Value)
            return Result<Guid>.Failure(Error.Forbidden("You can only add photos to your own pieces."));

        var imageKey = await imageStorage.UploadAsync(
            request.FileContent, request.ContentType,
            keyPrefix: $"portfolio/{artistId.Value:N}", cancellationToken);

        var now = clock.UtcNow;
        var photo = new SessionPhoto(
            id: Guid.NewGuid(),
            portfolioPieceId: piece.Id,
            order: request.Order,
            photoType: request.PhotoType,
            imageUrl: imageKey,
            uploadedByUserId: currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated artist must have a UserId claim."),
            uploadedByRole: UploadedByRole.Artist,
            uploadedAt: now,
            linkedSessionDate: request.LinkedSessionDate);
        photos.Add(photo);

        return Result<Guid>.Success(photo.Id);
    }
}

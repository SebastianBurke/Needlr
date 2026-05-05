using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.CreatePortfolioPiece;

internal sealed class CreatePortfolioPieceCommandHandler(
    IStudioAuthorization studioAuthorization,
    ICurrentUser currentUser,
    IPortfolioPieceRepository pieces,
    ISessionPhotoRepository photos,
    ITattooStyleRepository styles,
    IImageStorage imageStorage,
    IClock clock) : IRequestHandler<CreatePortfolioPieceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePortfolioPieceCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can create portfolio pieces."));

        var resolvedStyles = await styles.GetByIdsAsync(request.StyleIds, cancellationToken);
        if (resolvedStyles.Count != request.StyleIds.Count)
            return Result<Guid>.Failure(Error.Validation("One or more style ids are unknown."));

        var imageKey = await imageStorage.UploadAsync(
            request.FileContent, request.ContentType,
            keyPrefix: $"portfolio/{artistId.Value:N}", cancellationToken);

        var now = clock.UtcNow;
        var piece = new PortfolioPiece(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            bodyPlacement: request.BodyPlacement,
            yearCompleted: request.YearCompleted,
            createdAt: now,
            title: request.Title,
            description: request.Description,
            approximateSizeCm: request.ApproximateSizeCm,
            estimatedSessionLengthHours: request.EstimatedSessionLengthHours,
            progressionStatus: request.ProgressionStatus,
            linkedBookingId: request.LinkedBookingId);

        // Attach styles + freeform tags via the entity's collections.
        foreach (var s in resolvedStyles) piece.Styles.Add(s);
        foreach (var t in request.FreeformTags) piece.FreeformTags.Add(t.Trim().ToLowerInvariant());

        pieces.Add(piece);

        var photo = new SessionPhoto(
            id: Guid.NewGuid(),
            portfolioPieceId: piece.Id,
            order: 0,
            photoType: PhotoType.Fresh,
            imageUrl: imageKey,
            uploadedByUserId: currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated artist must have a UserId claim."),
            uploadedByRole: UploadedByRole.Artist,
            uploadedAt: now);
        photos.Add(photo);

        return Result<Guid>.Success(piece.Id);
    }
}

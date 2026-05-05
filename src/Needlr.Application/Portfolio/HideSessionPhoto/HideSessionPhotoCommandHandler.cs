using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Portfolio.HideSessionPhoto;

internal sealed class HideSessionPhotoCommandHandler(
    IStudioAuthorization studioAuthorization,
    ISessionPhotoRepository photos) : IRequestHandler<HideSessionPhotoCommand, Result>
{
    public async Task<Result> Handle(HideSessionPhotoCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can hide session photos."));

        var loaded = await photos.GetByIdWithPieceAsync(request.PhotoId, cancellationToken);
        if (loaded is null)
            return Result.Failure(Error.NotFound("SessionPhoto"));
        var (photo, piece) = loaded.Value;

        if (piece.ArtistId != artistId.Value)
            return Result.Failure(Error.Forbidden("You can only hide photos on your own pieces."));

        photo.IsHidden = true;
        photo.HiddenReason = request.Reason.Trim();
        return Result.Success();
    }
}

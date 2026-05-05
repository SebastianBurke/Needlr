using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Portfolio.DeletePortfolioPiece;

internal sealed class DeletePortfolioPieceCommandHandler(
    IStudioAuthorization studioAuthorization,
    IPortfolioPieceRepository pieces) : IRequestHandler<DeletePortfolioPieceCommand, Result>
{
    public async Task<Result> Handle(DeletePortfolioPieceCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can delete portfolio pieces."));

        var piece = await pieces.GetByIdAsync(request.PortfolioPieceId, cancellationToken);
        if (piece is null)
            return Result.Failure(Error.NotFound("PortfolioPiece"));

        if (piece.ArtistId != artistId.Value)
            return Result.Failure(Error.Forbidden("You can only delete your own pieces."));

        // EF cascade removes Sessions (via owned FK from SessionPhoto). The blob storage is
        // not deleted here — orphan blob cleanup is a separate concern and is deferred to
        // Phase 14 retention jobs (the same job that purges BookingAttachment blobs at 1y can
        // grow a parallel sweep for orphan portfolio blobs if it becomes a real cost).
        pieces.Remove(piece);
        return Result.Success();
    }
}

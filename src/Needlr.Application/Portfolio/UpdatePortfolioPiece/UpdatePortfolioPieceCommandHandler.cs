using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Portfolio.UpdatePortfolioPiece;

internal sealed class UpdatePortfolioPieceCommandHandler(
    IStudioAuthorization studioAuthorization,
    IPortfolioPieceRepository pieces,
    ITattooStyleRepository styles) : IRequestHandler<UpdatePortfolioPieceCommand, Result>
{
    public async Task<Result> Handle(UpdatePortfolioPieceCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can edit portfolio pieces."));

        var piece = await pieces.GetByIdWithDetailsAsync(request.PortfolioPieceId, cancellationToken);
        if (piece is null)
            return Result.Failure(Error.NotFound("PortfolioPiece"));

        if (piece.ArtistId != artistId.Value)
            return Result.Failure(Error.Forbidden("You can only edit your own pieces."));

        var resolvedStyles = await styles.GetByIdsAsync(request.StyleIds, cancellationToken);
        if (resolvedStyles.Count != request.StyleIds.Count)
            return Result.Failure(Error.Validation("One or more style ids are unknown."));

        piece.Title = request.Title?.Trim();
        piece.Description = request.Description?.Trim();
        piece.BodyPlacement = request.BodyPlacement;
        piece.ApproximateSizeCm = request.ApproximateSizeCm;
        piece.EstimatedSessionLengthHours = request.EstimatedSessionLengthHours;
        piece.YearCompleted = request.YearCompleted;
        piece.ProgressionStatus = request.ProgressionStatus;

        // Replace styles + freeform tags wholesale.
        piece.Styles.Clear();
        foreach (var s in resolvedStyles) piece.Styles.Add(s);
        piece.FreeformTags.Clear();
        foreach (var t in request.FreeformTags) piece.FreeformTags.Add(t.Trim().ToLowerInvariant());

        return Result.Success();
    }
}

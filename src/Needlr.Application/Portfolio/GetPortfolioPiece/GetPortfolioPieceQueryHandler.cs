using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Portfolio.GetPortfolioPiece;

internal sealed class GetPortfolioPieceQueryHandler(IPortfolioPieceRepository pieces)
    : IRequestHandler<GetPortfolioPieceQuery, Result<PortfolioPieceDto>>
{
    public async Task<Result<PortfolioPieceDto>> Handle(
        GetPortfolioPieceQuery request, CancellationToken cancellationToken)
    {
        var piece = await pieces.GetByIdWithDetailsAsync(request.PortfolioPieceId, cancellationToken);
        if (piece is null)
            return Result<PortfolioPieceDto>.Failure(Error.NotFound("PortfolioPiece"));

        var photos = piece.Sessions
            .OrderBy(s => s.Order)
            .Select(s => new SessionPhotoDto(
                s.Id, s.Order, s.PhotoType, s.ImageUrl,
                s.UploadedByUserId, s.UploadedByRole, s.UploadedAt,
                s.LinkedSessionDate, s.IsHidden, s.HiddenReason))
            .ToList();

        var styles = piece.Styles
            .Select(t => new TattooStyleDto(t.Id, t.Name, t.Slug, t.IsCanonical))
            .ToList();

        var dto = new PortfolioPieceDto(
            piece.Id, piece.ArtistId, piece.Title, piece.Description, piece.BodyPlacement,
            piece.ApproximateSizeCm, piece.EstimatedSessionLengthHours, piece.YearCompleted,
            piece.ProgressionStatus, piece.LinkedBookingId, piece.CreatedAt,
            styles, piece.FreeformTags.ToList(), photos);

        return Result<PortfolioPieceDto>.Success(dto);
    }
}

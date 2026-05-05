using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Portfolio.GetArtistPortfolio;

internal sealed class GetArtistPortfolioQueryHandler(IPortfolioPieceRepository pieces)
    : IRequestHandler<GetArtistPortfolioQuery, Result<PagedResult<PortfolioPieceSummaryDto>>>
{
    public async Task<Result<PagedResult<PortfolioPieceSummaryDto>>> Handle(
        GetArtistPortfolioQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page.Clamp();
        var total = await pieces.CountByArtistAsync(request.ArtistId, cancellationToken);
        var rows = await pieces.ListByArtistAsync(request.ArtistId, page.Skip, page.PageSize, cancellationToken);

        var items = rows.Select(p => new PortfolioPieceSummaryDto(
            p.Id, p.ArtistId, p.Title, p.BodyPlacement, p.YearCompleted, p.ProgressionStatus, p.CreatedAt,
            FreshPhotoUrl: p.Sessions.FirstOrDefault(s => s.PhotoType == PhotoType.Fresh && !s.IsHidden)?.ImageUrl,
            HealedPhotoUrl: p.Sessions.FirstOrDefault(s => s.PhotoType == PhotoType.Healed && !s.IsHidden)?.ImageUrl))
            .ToList();

        return Result<PagedResult<PortfolioPieceSummaryDto>>.Success(
            new PagedResult<PortfolioPieceSummaryDto>(items, page.Page, page.PageSize, total));
    }
}

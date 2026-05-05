using Needlr.Application.Common.Pagination;
using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.GetArtistPortfolio;

public sealed record GetArtistPortfolioQuery(Guid ArtistId, PageRequest Page)
    : IQuery<PagedResult<PortfolioPieceSummaryDto>>;

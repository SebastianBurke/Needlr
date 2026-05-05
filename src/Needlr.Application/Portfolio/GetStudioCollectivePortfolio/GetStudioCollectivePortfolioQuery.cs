using Needlr.Application.Common.Pagination;
using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.GetStudioCollectivePortfolio;

public sealed record GetStudioCollectivePortfolioQuery(Guid StudioId, PageRequest Page)
    : IQuery<PagedResult<PortfolioPieceSummaryDto>>;

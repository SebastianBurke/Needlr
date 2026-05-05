using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Discovery.SearchStudios;

internal sealed class SearchStudiosQueryHandler(IArtistDiscoveryService discovery)
    : IRequestHandler<SearchStudiosQuery, Result<PagedResult<DiscoveryStudioDto>>>
{
    public async Task<Result<PagedResult<DiscoveryStudioDto>>> Handle(
        SearchStudiosQuery request, CancellationToken cancellationToken)
    {
        var page = await discovery.SearchAsync(request.Criteria, cancellationToken);
        return Result<PagedResult<DiscoveryStudioDto>>.Success(page);
    }
}

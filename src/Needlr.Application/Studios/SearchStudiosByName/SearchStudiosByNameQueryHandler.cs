using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Studios.SearchStudiosByName;

internal sealed class SearchStudiosByNameQueryHandler(IStudioRepository studios)
    : IRequestHandler<SearchStudiosByNameQuery, Result<IReadOnlyList<StudioSummaryDto>>>
{
    private const int MaxTake = 100;

    public async Task<Result<IReadOnlyList<StudioSummaryDto>>> Handle(
        SearchStudiosByNameQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, MaxTake);
        var results = await studios.SearchByNameAsync(request.Query, take, cancellationToken);

        var dtos = results.Select(s => new StudioSummaryDto(
            s.Id, s.Name, s.Address, s.StudioType, s.Location.ToGeoPoint())).ToList();
        return Result<IReadOnlyList<StudioSummaryDto>>.Success(dtos);
    }
}

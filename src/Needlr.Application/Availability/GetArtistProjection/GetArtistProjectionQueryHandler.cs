using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.GetArtistProjection;

internal sealed class GetArtistProjectionQueryHandler(
    IArtistRepository artists,
    IArtistAvailabilityProjectionRepository projections)
    : IRequestHandler<GetArtistProjectionQuery, Result<IReadOnlyList<ProjectionDayDto>>>
{
    public async Task<Result<IReadOnlyList<ProjectionDayDto>>> Handle(
        GetArtistProjectionQuery request, CancellationToken cancellationToken)
    {
        if (request.To < request.From)
            return Result<IReadOnlyList<ProjectionDayDto>>.Failure(
                Error.Validation("`to` must be on or after `from`."));
        if (!await artists.ExistsAsync(request.ArtistId, cancellationToken))
            return Result<IReadOnlyList<ProjectionDayDto>>.Failure(Error.NotFound("Artist"));

        var rows = await projections.ListAsync(request.ArtistId, request.From, request.To, cancellationToken);
        IReadOnlyList<ProjectionDayDto> result = rows
            .OrderBy(p => p.Date)
            .Select(p => new ProjectionDayDto(p.Date, p.IsBookable, p.RemainingSessionHours))
            .ToList();
        return Result<IReadOnlyList<ProjectionDayDto>>.Success(result);
    }
}

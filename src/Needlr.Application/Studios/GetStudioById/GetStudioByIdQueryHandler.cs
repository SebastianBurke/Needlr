using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Studios.GetStudioById;

internal sealed class GetStudioByIdQueryHandler(IStudioRepository studios)
    : IRequestHandler<GetStudioByIdQuery, Result<StudioDto>>
{
    public async Task<Result<StudioDto>> Handle(GetStudioByIdQuery request, CancellationToken cancellationToken)
    {
        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result<StudioDto>.Failure(Error.NotFound("Studio"));

        return Result<StudioDto>.Success(new StudioDto(
            studio.Id,
            studio.Name,
            studio.StudioType,
            studio.Location.ToGeoPoint(),
            studio.Address,
            studio.JoinPolicy,
            studio.Description,
            studio.CreatedByArtistId));
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Application.Studios;

namespace Needlr.Application.Affiliations.GetMyAffiliations;

internal sealed class GetMyAffiliationsQueryHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations,
    IStudioRepository studios)
    : IRequestHandler<GetMyAffiliationsQuery, Result<IReadOnlyList<AffiliationDto>>>
{
    public async Task<Result<IReadOnlyList<AffiliationDto>>> Handle(
        GetMyAffiliationsQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<AffiliationDto>>.Failure(Error.Forbidden("Only artists have affiliations."));

        var rows = await affiliations.ListByArtistAsync(artistId.Value, cancellationToken);

        var dtos = new List<AffiliationDto>(rows.Count);
        foreach (var aff in rows)
        {
            var studio = await studios.GetByIdAsync(aff.StudioId, cancellationToken);
            if (studio is null) continue;
            dtos.Add(new AffiliationDto(
                aff.Id,
                aff.ArtistId,
                aff.StudioId,
                studio.Name,
                aff.Role,
                aff.AffiliationType,
                aff.Status,
                aff.StartDate,
                aff.EndDate,
                aff.IsPrimary));
        }

        return Result<IReadOnlyList<AffiliationDto>>.Success(dtos);
    }
}

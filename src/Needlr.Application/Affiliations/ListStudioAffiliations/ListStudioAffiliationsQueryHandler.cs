using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Affiliations.ListStudioAffiliations;

internal sealed class ListStudioAffiliationsQueryHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations,
    IArtistRepository artists)
    : IRequestHandler<ListStudioAffiliationsQuery, Result<IReadOnlyList<StudioAffiliationDetailDto>>>
{
    public async Task<Result<IReadOnlyList<StudioAffiliationDetailDto>>> Handle(
        ListStudioAffiliationsQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<StudioAffiliationDetailDto>>.Failure(Error.Forbidden("Only artists can view a studio roster's admin view."));

        var isAdmin = await affiliations.IsAdminAsync(artistId.Value, request.StudioId, cancellationToken);
        if (!isAdmin)
            return Result<IReadOnlyList<StudioAffiliationDetailDto>>.Failure(Error.Forbidden("Studio admin only."));

        var rows = await affiliations.ListByStudioAsync(request.StudioId, status: null, cancellationToken);

        var entries = new List<StudioAffiliationDetailDto>(rows.Count);
        foreach (var aff in rows)
        {
            var artist = await artists.GetByIdAsync(aff.ArtistId, cancellationToken);
            if (artist is null) continue;
            entries.Add(new StudioAffiliationDetailDto(
                aff.Id,
                aff.ArtistId,
                artist.DisplayName,
                aff.Role.ToString(),
                aff.AffiliationType.ToString(),
                aff.Status.ToString(),
                aff.StartDate,
                aff.EndDate,
                aff.IsPrimary));
        }

        return Result<IReadOnlyList<StudioAffiliationDetailDto>>.Success(entries);
    }
}

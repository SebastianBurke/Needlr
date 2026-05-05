using MediatR;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Studios.GetStudioRoster;

internal sealed class GetStudioRosterQueryHandler(
    IStudioRepository studios,
    IArtistStudioAffiliationRepository affiliations,
    IArtistRepository artists)
    : IRequestHandler<GetStudioRosterQuery, Result<StudioRosterDto>>
{
    public async Task<Result<StudioRosterDto>> Handle(GetStudioRosterQuery request, CancellationToken cancellationToken)
    {
        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result<StudioRosterDto>.Failure(Error.NotFound("Studio"));

        var rows = await affiliations.ListByStudioAsync(request.StudioId, AffiliationStatus.Active, cancellationToken);

        var entries = new List<StudioRosterEntryDto>(rows.Count);
        foreach (var aff in rows)
        {
            var artist = await artists.GetByIdAsync(aff.ArtistId, cancellationToken);
            if (artist is null) continue;
            entries.Add(new StudioRosterEntryDto(
                aff.Id,
                aff.ArtistId,
                artist.DisplayName,
                aff.Role,
                aff.AffiliationType,
                aff.StartDate,
                aff.EndDate,
                aff.IsPrimary));
        }

        return Result<StudioRosterDto>.Success(new StudioRosterDto(studio.Id, studio.Name, entries));
    }
}

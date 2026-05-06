using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.GetMyAvailability;

internal sealed class GetMyLeadTimesQueryHandler(
    IStudioAuthorization studioAuthorization,
    IArtistLeadTimeRepository leadTimes)
    : IRequestHandler<GetMyLeadTimesQuery, Result<IReadOnlyList<LeadTimeDto>>>
{
    public async Task<Result<IReadOnlyList<LeadTimeDto>>> Handle(
        GetMyLeadTimesQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<LeadTimeDto>>.Failure(
                Error.Forbidden("Only artists have lead times."));

        var rows = await leadTimes.ListByArtistAsync(artistId.Value, cancellationToken);
        IReadOnlyList<LeadTimeDto> result = rows
            .Select(lt => new LeadTimeDto(lt.BookingType, lt.MinimumDays))
            .ToList();
        return Result<IReadOnlyList<LeadTimeDto>>.Success(result);
    }
}

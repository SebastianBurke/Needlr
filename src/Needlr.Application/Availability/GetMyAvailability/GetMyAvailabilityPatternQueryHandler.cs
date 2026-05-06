using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.GetMyAvailability;

internal sealed class GetMyAvailabilityPatternQueryHandler(
    IStudioAuthorization studioAuthorization,
    IAvailabilityPatternRepository patterns)
    : IRequestHandler<GetMyAvailabilityPatternQuery, Result<IReadOnlyList<AvailabilityPatternDayDto>>>
{
    public async Task<Result<IReadOnlyList<AvailabilityPatternDayDto>>> Handle(
        GetMyAvailabilityPatternQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<AvailabilityPatternDayDto>>.Failure(
                Error.Forbidden("Only artists have an availability pattern."));

        var rows = await patterns.ListByArtistAsync(artistId.Value, cancellationToken);
        IReadOnlyList<AvailabilityPatternDayDto> result = rows
            .Select(p => new AvailabilityPatternDayDto(
                p.Id, p.DayOfWeek, p.Status, p.MaxSessionHours, p.EffectiveFrom, p.EffectiveUntil))
            .ToList();
        return Result<IReadOnlyList<AvailabilityPatternDayDto>>.Success(result);
    }
}

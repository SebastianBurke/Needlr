using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.GetMyAvailability;

internal sealed class GetMyAvailabilityOverridesQueryHandler(
    IStudioAuthorization studioAuthorization,
    IAvailabilityOverrideRepository overrides)
    : IRequestHandler<GetMyAvailabilityOverridesQuery, Result<IReadOnlyList<AvailabilityOverrideDto>>>
{
    public async Task<Result<IReadOnlyList<AvailabilityOverrideDto>>> Handle(
        GetMyAvailabilityOverridesQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<AvailabilityOverrideDto>>.Failure(
                Error.Forbidden("Only artists have overrides."));

        var rows = await overrides.ListByArtistAsync(artistId.Value, request.From, request.To, cancellationToken);
        IReadOnlyList<AvailabilityOverrideDto> result = rows
            .Select(o => new AvailabilityOverrideDto(o.Id, o.Date, o.Status, o.MaxSessionHours, o.Reason))
            .ToList();
        return Result<IReadOnlyList<AvailabilityOverrideDto>>.Success(result);
    }
}

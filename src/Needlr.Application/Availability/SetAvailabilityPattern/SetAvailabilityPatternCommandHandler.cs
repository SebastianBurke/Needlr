using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Availability;

namespace Needlr.Application.Availability.SetAvailabilityPattern;

internal sealed class SetAvailabilityPatternCommandHandler(
    IStudioAuthorization studioAuthorization,
    IAvailabilityPatternRepository patterns,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<SetAvailabilityPatternCommand, Result>
{
    public async Task<Result> Handle(SetAvailabilityPatternCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can set availability."));

        var existing = await patterns.ListByArtistAsync(artistId.Value, cancellationToken);
        if (existing.Count > 0)
            patterns.RemoveRange(existing);

        var today = DateOnly.FromDateTime(clock.UtcNow);
        foreach (var day in request.Days)
        {
            patterns.Add(new AvailabilityPattern(
                id: Guid.NewGuid(),
                artistId: artistId.Value,
                dayOfWeek: day.DayOfWeek,
                status: day.Status,
                effectiveFrom: day.EffectiveFrom ?? today,
                effectiveUntil: day.EffectiveUntil,
                maxSessionHours: day.MaxSessionHours));
        }

        // Flush before the projector runs so its reads see the just-added rows. EF's
        // tracked-but-unsaved entities are not visible to LINQ queries by default.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);

        return Result.Success();
    }
}

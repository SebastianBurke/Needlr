using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.RemoveAvailabilityOverride;

internal sealed class RemoveAvailabilityOverrideCommandHandler(
    IStudioAuthorization studioAuthorization,
    IAvailabilityOverrideRepository overrides,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveAvailabilityOverrideCommand, Result>
{
    public async Task<Result> Handle(RemoveAvailabilityOverrideCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can clear overrides."));

        var existing = await overrides.GetAsync(artistId.Value, request.Date, cancellationToken);
        if (existing is null)
            return Result.Failure(Error.NotFound("AvailabilityOverride"));

        overrides.Remove(existing);
        // Flush before the projector runs so it doesn't see the soon-to-be-deleted row.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result.Success();
    }
}

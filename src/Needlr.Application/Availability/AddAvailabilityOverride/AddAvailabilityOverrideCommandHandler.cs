using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Availability;

namespace Needlr.Application.Availability.AddAvailabilityOverride;

internal sealed class AddAvailabilityOverrideCommandHandler(
    IStudioAuthorization studioAuthorization,
    IAvailabilityOverrideRepository overrides,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork) : IRequestHandler<AddAvailabilityOverrideCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddAvailabilityOverrideCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can set availability."));

        // Replace-by-date semantics: at most one override per (artist, date). The unique index
        // backs this — but we delete first to avoid colliding with the artist's existing row.
        var existing = await overrides.GetAsync(artistId.Value, request.Date, cancellationToken);
        if (existing is not null)
            overrides.Remove(existing);

        var added = new AvailabilityOverride(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            date: request.Date,
            status: request.Status,
            maxSessionHours: request.MaxSessionHours,
            reason: request.Reason);
        overrides.Add(added);

        // Flush before the projector runs so it sees the new override.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);

        return Result<Guid>.Success(added.Id);
    }
}

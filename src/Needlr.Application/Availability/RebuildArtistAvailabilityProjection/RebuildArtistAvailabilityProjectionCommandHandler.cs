using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.RebuildArtistAvailabilityProjection;

internal sealed class RebuildArtistAvailabilityProjectionCommandHandler(
    IArtistRepository artists,
    IAvailabilityProjector projector) : IRequestHandler<RebuildArtistAvailabilityProjectionCommand, Result>
{
    public async Task<Result> Handle(
        RebuildArtistAvailabilityProjectionCommand request, CancellationToken cancellationToken)
    {
        if (!await artists.ExistsAsync(request.ArtistId, cancellationToken))
            return Result.Failure(Error.NotFound("Artist"));

        await projector.RebuildRollingWindowAsync(request.ArtistId, cancellationToken);
        return Result.Success();
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.UpdateArtistProfile;

internal sealed class UpdateArtistProfileCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists)
    : IRequestHandler<UpdateArtistProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateArtistProfileCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can update an artist profile."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result.Failure(Error.NotFound("Artist"));

        artist.Bio = request.Bio ?? string.Empty;
        artist.YearsExperience = request.YearsExperience;
        artist.HourlyRateCad = request.HourlyRateCad;
        artist.ShopMinimumCad = request.ShopMinimumCad;
        artist.CancellationPolicy = request.CancellationPolicy;
        // Save handled by TransactionBehavior pipeline.
        return Result.Success();
    }
}

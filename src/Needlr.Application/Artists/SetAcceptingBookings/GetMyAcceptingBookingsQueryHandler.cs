using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.SetAcceptingBookings;

internal sealed class GetMyAcceptingBookingsQueryHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists)
    : IRequestHandler<GetMyAcceptingBookingsQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(GetMyAcceptingBookingsQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<bool>.Failure(Error.Forbidden("Only artists can read accepting-bookings."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result<bool>.Failure(Error.NotFound("Artist"));

        return Result<bool>.Success(artist.AcceptingNewBookings);
    }
}

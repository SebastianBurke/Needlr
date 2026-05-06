using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.SetAcceptingBookings;

internal sealed class SetAcceptingBookingsCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists)
    : IRequestHandler<SetAcceptingBookingsCommand, Result>
{
    public async Task<Result> Handle(SetAcceptingBookingsCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can change accepting-bookings."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result.Failure(Error.NotFound("Artist"));

        artist.AcceptingNewBookings = request.Accepting;
        // Save handled by the TransactionBehavior pipeline.
        return Result.Success();
    }
}

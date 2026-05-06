using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.CloseBookingWindow;

internal sealed class CloseBookingWindowCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingWindowRepository windows,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork) : IRequestHandler<CloseBookingWindowCommand, Result>
{
    public async Task<Result> Handle(CloseBookingWindowCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can close booking windows."));

        var window = await windows.GetByIdAsync(request.WindowId, cancellationToken);
        if (window is null)
            return Result.Failure(Error.NotFound("BookingWindow"));
        if (window.ArtistId != artistId.Value)
            return Result.Failure(Error.Forbidden("Not your booking window."));

        windows.Remove(window);
        // Flush before the projector runs so it doesn't see the soon-to-be-deleted window.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result.Success();
    }
}

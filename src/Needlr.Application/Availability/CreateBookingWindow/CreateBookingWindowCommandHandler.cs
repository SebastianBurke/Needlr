using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Availability;

namespace Needlr.Application.Availability.CreateBookingWindow;

internal sealed class CreateBookingWindowCommandHandler(
    IStudioAuthorization studioAuthorization,
    IBookingWindowRepository windows,
    IAvailabilityProjector projector,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBookingWindowCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookingWindowCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can create booking windows."));

        var window = new BookingWindow(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            windowOpensAt: request.WindowOpensAt,
            windowClosesAt: request.WindowClosesAt,
            targetRangeStart: request.TargetRangeStart,
            targetRangeEnd: request.TargetRangeEnd);
        windows.Add(window);

        // Flush before the projector runs so it sees the new window.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await projector.RebuildRollingWindowAsync(artistId.Value, cancellationToken);
        return Result<Guid>.Success(window.Id);
    }
}

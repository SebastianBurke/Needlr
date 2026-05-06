using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.GetMyAvailability;

internal sealed class GetMyBookingWindowsQueryHandler(
    IStudioAuthorization studioAuthorization,
    IBookingWindowRepository windows)
    : IRequestHandler<GetMyBookingWindowsQuery, Result<IReadOnlyList<BookingWindowDto>>>
{
    public async Task<Result<IReadOnlyList<BookingWindowDto>>> Handle(
        GetMyBookingWindowsQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<IReadOnlyList<BookingWindowDto>>.Failure(
                Error.Forbidden("Only artists have booking windows."));

        var rows = await windows.ListByArtistAsync(artistId.Value, cancellationToken);
        IReadOnlyList<BookingWindowDto> result = rows
            .Select(w => new BookingWindowDto(
                w.Id, w.WindowOpensAt, w.WindowClosesAt, w.TargetRangeStart, w.TargetRangeEnd))
            .ToList();
        return Result<IReadOnlyList<BookingWindowDto>>.Success(result);
    }
}

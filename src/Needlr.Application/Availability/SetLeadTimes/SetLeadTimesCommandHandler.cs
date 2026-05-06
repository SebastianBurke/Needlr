using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Identity;

namespace Needlr.Application.Availability.SetLeadTimes;

internal sealed class SetLeadTimesCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistLeadTimeRepository leadTimes) : IRequestHandler<SetLeadTimesCommand, Result>
{
    public async Task<Result> Handle(SetLeadTimesCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result.Failure(Error.Forbidden("Only artists can set lead times."));

        var existing = await leadTimes.ListByArtistAsync(artistId.Value, cancellationToken);
        foreach (var row in existing)
            leadTimes.Remove(row);

        foreach (var input in request.LeadTimes)
        {
            leadTimes.Add(new ArtistLeadTime(
                id: Guid.NewGuid(),
                artistId: artistId.Value,
                bookingType: input.BookingType,
                minimumDays: input.MinimumDays));
        }

        return Result.Success();
    }
}

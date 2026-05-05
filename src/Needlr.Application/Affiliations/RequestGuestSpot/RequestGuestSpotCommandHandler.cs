using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Application.Affiliations.RequestGuestSpot;

internal sealed class RequestGuestSpotCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<RequestGuestSpotCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestGuestSpotCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can request guest spots."));

        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result<Guid>.Failure(Error.NotFound("Studio"));

        // Existing PERMANENT affiliations are independent of guest spots — but a pending guest
        // spot would conflict (per FEATURE_SPECS.md a guest spot is a discrete request).
        var existing = await affiliations.GetByArtistAndStudioAsync(artistId.Value, studio.Id, cancellationToken);
        if (existing is { Status: AffiliationStatus.Pending })
            return Result<Guid>.Failure(Error.Conflict("You already have a pending request for this studio."));

        var spot = new ArtistStudioAffiliation(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            studioId: studio.Id,
            role: AffiliationRole.Member,
            affiliationType: AffiliationType.GuestSpot,
            startDate: request.StartDate,
            endDate: request.EndDate,
            status: AffiliationStatus.Pending);
        affiliations.Add(spot);

        return Result<Guid>.Success(spot.Id);
    }
}

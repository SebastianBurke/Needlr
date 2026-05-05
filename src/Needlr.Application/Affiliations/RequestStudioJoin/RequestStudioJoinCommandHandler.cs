using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Application.Affiliations.RequestStudioJoin;

internal sealed class RequestStudioJoinCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios,
    IArtistStudioAffiliationRepository affiliations,
    IClock clock) : IRequestHandler<RequestStudioJoinCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestStudioJoinCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can request to join studios."));

        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result<Guid>.Failure(Error.NotFound("Studio"));

        if (studio.JoinPolicy != JoinPolicy.Open)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "This studio doesn't accept join requests. Ask an admin for an invitation."));

        var existing = await affiliations.GetByArtistAndStudioAsync(artistId.Value, studio.Id, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(Error.Conflict("You already have an affiliation with this studio."));

        var affiliation = new ArtistStudioAffiliation(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            studioId: studio.Id,
            role: AffiliationRole.Member,
            affiliationType: AffiliationType.Permanent,
            startDate: DateOnly.FromDateTime(clock.UtcNow),
            status: AffiliationStatus.Pending);
        affiliations.Add(affiliation);

        return Result<Guid>.Success(affiliation.Id);
    }
}

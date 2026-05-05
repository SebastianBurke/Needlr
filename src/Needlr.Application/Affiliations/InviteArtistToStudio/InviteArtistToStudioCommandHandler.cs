using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Application.Affiliations.InviteArtistToStudio;

internal sealed class InviteArtistToStudioCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios,
    IArtistRepository artists,
    IArtistStudioAffiliationRepository affiliations,
    IClock clock) : IRequestHandler<InviteArtistToStudioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(InviteArtistToStudioCommand request, CancellationToken cancellationToken)
    {
        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(request.StudioId, cancellationToken))
            return Result<Guid>.Failure(Error.Forbidden("You must be an admin of this studio."));

        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result<Guid>.Failure(Error.NotFound("Studio"));

        if (studio.JoinPolicy == JoinPolicy.Closed)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "This studio is closed to new members. Change the join policy first."));

        if (!await artists.ExistsAsync(request.ArtistId, cancellationToken))
            return Result<Guid>.Failure(Error.NotFound("Artist"));

        var existing = await affiliations.GetByArtistAndStudioAsync(request.ArtistId, request.StudioId, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(Error.Conflict("This artist already has an affiliation with the studio."));

        var invite = new ArtistStudioAffiliation(
            id: Guid.NewGuid(),
            artistId: request.ArtistId,
            studioId: studio.Id,
            role: AffiliationRole.Member,
            affiliationType: AffiliationType.Permanent,
            startDate: DateOnly.FromDateTime(clock.UtcNow),
            status: AffiliationStatus.Pending);
        affiliations.Add(invite);

        return Result<Guid>.Success(invite.Id);
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.RemoveAffiliation;

internal sealed class RemoveAffiliationCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations,
    IClock clock) : IRequestHandler<RemoveAffiliationCommand, Result>
{
    public async Task<Result> Handle(RemoveAffiliationCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        var currentArtistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        var isStudioAdmin = await studioAuthorization.IsCurrentUserStudioAdminAsync(aff.StudioId, cancellationToken);
        var isSelf = currentArtistId == aff.ArtistId;

        if (!isStudioAdmin && !isSelf)
            return Result.Failure(Error.Forbidden(
                "You can only remove an affiliation if you're a studio admin or the affiliated artist."));

        if (aff.Role == AffiliationRole.Founder)
            return Result.Failure(Error.FailedPrecondition(
                "The founder cannot leave without first ceding founder status."));

        aff.Status = AffiliationStatus.Ended;
        aff.EndDate = DateOnly.FromDateTime(clock.UtcNow);
        return Result.Success();
    }
}

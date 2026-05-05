using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.RespondToJoinRequest;

internal sealed class RespondToJoinRequestCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<RespondToJoinRequestCommand, Result>
{
    public async Task<Result> Handle(RespondToJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(aff.StudioId, cancellationToken))
            return Result.Failure(Error.Forbidden("You must be an admin of this studio."));

        if (aff.Status != AffiliationStatus.Pending)
            return Result.Failure(Error.FailedPrecondition(
                "Only pending affiliations can be approved or rejected."));

        if (aff.AffiliationType == AffiliationType.GuestSpot)
            return Result.Failure(Error.FailedPrecondition(
                "Use the guest-spot response endpoint for guest-spot requests."));

        // StartDate stays as the original request date (init-only on the entity); the Active
        // status alone signifies the affiliation is now in effect.
        aff.Status = request.Accept ? AffiliationStatus.Active : AffiliationStatus.Rejected;
        return Result.Success();
    }
}

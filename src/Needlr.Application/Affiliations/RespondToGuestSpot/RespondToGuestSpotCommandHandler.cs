using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.RespondToGuestSpot;

internal sealed class RespondToGuestSpotCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<RespondToGuestSpotCommand, Result>
{
    public async Task<Result> Handle(RespondToGuestSpotCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        if (aff.AffiliationType != AffiliationType.GuestSpot)
            return Result.Failure(Error.FailedPrecondition(
                "This is not a guest-spot affiliation. Use the join-request response endpoint."));

        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(aff.StudioId, cancellationToken))
            return Result.Failure(Error.Forbidden("You must be an admin of this studio."));

        if (aff.Status != AffiliationStatus.Pending)
            return Result.Failure(Error.FailedPrecondition(
                "Only pending guest-spot requests can be approved or rejected."));

        aff.Status = request.Accept ? AffiliationStatus.Active : AffiliationStatus.Rejected;
        return Result.Success();
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.RespondToStudioInvitation;

internal sealed class RespondToStudioInvitationCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<RespondToStudioInvitationCommand, Result>
{
    public async Task<Result> Handle(RespondToStudioInvitationCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId != aff.ArtistId)
            return Result.Failure(Error.Forbidden("You can only respond to your own invitations."));

        if (aff.Status != AffiliationStatus.Pending)
            return Result.Failure(Error.FailedPrecondition(
                "Only pending invitations can be accepted or declined."));

        aff.Status = request.Accept ? AffiliationStatus.Active : AffiliationStatus.Rejected;
        return Result.Success();
    }
}

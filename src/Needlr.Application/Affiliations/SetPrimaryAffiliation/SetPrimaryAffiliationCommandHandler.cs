using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.SetPrimaryAffiliation;

internal sealed class SetPrimaryAffiliationCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<SetPrimaryAffiliationCommand, Result>
{
    public async Task<Result> Handle(SetPrimaryAffiliationCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId != aff.ArtistId)
            return Result.Failure(Error.Forbidden("You can only set your own primary affiliation."));

        if (aff.Status != AffiliationStatus.Active)
            return Result.Failure(Error.FailedPrecondition(
                "Only active affiliations can be marked as primary."));

        // Clear primary on all of this artist's other affiliations and mark this one.
        var all = await affiliations.ListByArtistAsync(artistId.Value, cancellationToken);
        foreach (var other in all)
            other.IsPrimary = (other.Id == aff.Id);

        return Result.Success();
    }
}

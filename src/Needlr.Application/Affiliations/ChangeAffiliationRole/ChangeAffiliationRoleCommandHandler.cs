using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.ChangeAffiliationRole;

internal sealed class ChangeAffiliationRoleCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistStudioAffiliationRepository affiliations) : IRequestHandler<ChangeAffiliationRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeAffiliationRoleCommand request, CancellationToken cancellationToken)
    {
        var aff = await affiliations.GetByIdAsync(request.AffiliationId, cancellationToken);
        if (aff is null)
            return Result.Failure(Error.NotFound("Affiliation"));

        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(aff.StudioId, cancellationToken))
            return Result.Failure(Error.Forbidden("You must be an admin of this studio."));

        if (aff.Status != AffiliationStatus.Active)
            return Result.Failure(Error.FailedPrecondition("Only active affiliations can have their role changed."));

        if (aff.Role == AffiliationRole.Founder)
            return Result.Failure(Error.FailedPrecondition(
                "The founder cannot be demoted directly. Cede founder status to another admin first."));

        aff.Role = request.NewRole;
        return Result.Success();
    }
}

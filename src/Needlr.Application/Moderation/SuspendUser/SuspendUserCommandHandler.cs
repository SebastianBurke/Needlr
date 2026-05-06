using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Moderation.SuspendUser;

internal sealed class SuspendUserCommandHandler(
    ICurrentUser currentUser,
    IModerationService moderation) : IRequestHandler<SuspendUserCommand, Result>
{
    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return Result.Failure(Error.Forbidden("Admin only."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(Error.Validation("Reason is required."));

        var ok = await moderation.SuspendAsync(request.UserId, request.Reason, cancellationToken);
        return ok ? Result.Success() : Result.Failure(Error.NotFound("User"));
    }
}

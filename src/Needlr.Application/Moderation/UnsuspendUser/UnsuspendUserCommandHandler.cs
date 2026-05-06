using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Moderation.UnsuspendUser;

internal sealed class UnsuspendUserCommandHandler(
    ICurrentUser currentUser,
    IModerationService moderation) : IRequestHandler<UnsuspendUserCommand, Result>
{
    public async Task<Result> Handle(UnsuspendUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return Result.Failure(Error.Forbidden("Admin only."));

        var ok = await moderation.UnsuspendAsync(request.UserId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(Error.NotFound("User"));
    }
}

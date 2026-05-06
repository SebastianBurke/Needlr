using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Moderation;

namespace Needlr.Application.Moderation.WarnUser;

internal sealed class WarnUserCommandHandler(
    ICurrentUser currentUser,
    IUserWarningRepository warnings,
    IClock clock) : IRequestHandler<WarnUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(WarnUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin) || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Forbidden("Admin only."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result<Guid>.Failure(Error.Validation("Reason is required."));

        var warning = new UserWarning(
            id: Guid.NewGuid(),
            userId: request.UserId,
            issuedByAdminId: currentUser.UserId.Value,
            reason: request.Reason,
            issuedAt: clock.UtcNow);
        warnings.Add(warning);
        await Task.CompletedTask;
        return Result<Guid>.Success(warning.Id);
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.HideMessage;

internal sealed class HideMessageCommandHandler(
    ICurrentUser currentUser,
    IMessageRepository messages) : IRequestHandler<HideMessageCommand, Result>
{
    public async Task<Result> Handle(HideMessageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return Result.Failure(Error.Forbidden("Admin only."));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(Error.Validation("Reason is required."));

        var message = await messages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return Result.Failure(Error.NotFound("Message"));

        // Per ADR-003 retention rules we can't lose the original body — it stays in the
        // ledger for audits/appeals. The on-the-wire body is replaced with a fixed notice.
        // Admin tooling in Phase 22 surfaces the original from a separate audit endpoint.
        message.Body = $"[message hidden by admin: {request.Reason.Trim()}]";
        message.IsReportedFlag = true;
        return Result.Success();
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.MessageThreads.GetUnreadCount;

internal sealed class GetUnreadMessageCountQueryHandler(
    ICurrentUser currentUser,
    IMessageRepository messages) : IRequestHandler<GetUnreadMessageCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetUnreadMessageCountQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<int>.Failure(Error.Unauthorized());

        var count = await messages.CountUnreadForUserAsync(currentUser.UserId.Value, cancellationToken);
        return Result<int>.Success(count);
    }
}

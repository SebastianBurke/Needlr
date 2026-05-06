using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using DomainPushSubscription = Needlr.Domain.Notifications.PushSubscription;

namespace Needlr.Application.Notifications.UnregisterPushSubscription;

internal sealed class UnregisterPushSubscriptionCommandHandler(
    ICurrentUser currentUser,
    IPushSubscriptionRepository subscriptions) : IRequestHandler<UnregisterPushSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        UnregisterPushSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized());

        var sub = await subscriptions.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure(Error.NotFound("PushSubscription"));
        if (sub.UserId != currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("Not your subscription."));

        subscriptions.Remove(sub);
        return Result.Success();
    }
}

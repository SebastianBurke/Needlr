using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using DomainPushSubscription = Needlr.Domain.Notifications.PushSubscription;

namespace Needlr.Application.Notifications.RegisterPushSubscription;

internal sealed class RegisterPushSubscriptionCommandHandler(
    ICurrentUser currentUser,
    IPushSubscriptionRepository subscriptions,
    IClock clock) : IRequestHandler<RegisterPushSubscriptionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RegisterPushSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        // Refresh-in-place if the same browser endpoint is re-registered (browser keys
        // rotate periodically; the endpoint URL stays stable for the subscription's life).
        var existing = await subscriptions.GetByEndpointAsync(userId, request.Endpoint, cancellationToken);
        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            return Result<Guid>.Success(existing.Id);
        }

        var sub = new DomainPushSubscription(
            id: Guid.NewGuid(),
            userId: userId,
            endpoint: request.Endpoint,
            p256dh: request.P256dh,
            auth: request.Auth,
            createdAt: clock.UtcNow);
        subscriptions.Add(sub);
        return Result<Guid>.Success(sub.Id);
    }
}

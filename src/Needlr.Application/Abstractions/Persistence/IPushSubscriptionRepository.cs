using DomainPushSubscription = Needlr.Domain.Notifications.PushSubscription;

namespace Needlr.Application.Abstractions.Persistence;

/// <summary>
/// Aliased to <c>DomainPushSubscription</c> because the parent namespace
/// <c>Needlr.Application.Abstractions</c> already exposes a sender-facing
/// <c>PushSubscription</c> record (the wire shape used by <c>IPushNotificationSender</c>);
/// shadowing collisions between the two types are why everything in this interface speaks
/// in the alias.
/// </summary>
public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<DomainPushSubscription>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<DomainPushSubscription?> GetByEndpointAsync(
        Guid userId, string endpoint, CancellationToken cancellationToken = default);

    Task<DomainPushSubscription?> GetByIdAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default);

    void Add(DomainPushSubscription subscription);
    void Remove(DomainPushSubscription subscription);
}

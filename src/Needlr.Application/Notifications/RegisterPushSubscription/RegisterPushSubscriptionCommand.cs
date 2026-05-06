using Needlr.Application.Messaging;

namespace Needlr.Application.Notifications.RegisterPushSubscription;

/// <summary>
/// Registers (or refreshes the keys for) a Web Push subscription. Idempotent on
/// (UserId, Endpoint): re-registering the same browser updates the keys instead of
/// creating duplicates.
/// </summary>
public sealed record RegisterPushSubscriptionCommand(
    string Endpoint, string P256dh, string Auth) : ICommand<Guid>;

using Needlr.Application.Messaging;

namespace Needlr.Application.Notifications.UnregisterPushSubscription;

public sealed record UnregisterPushSubscriptionCommand(Guid SubscriptionId) : ICommand;

using System.Collections.Concurrent;
using Needlr.Application.Abstractions;

namespace Needlr.Api.IntegrationTests.Fixtures;

public sealed class RecordingPushSender : IPushNotificationSender
{
    public ConcurrentBag<(string Endpoint, string Payload)> Sent { get; } = new();

    public Task SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default)
    {
        Sent.Add((subscription.Endpoint, payload));
        return Task.CompletedTask;
    }
}

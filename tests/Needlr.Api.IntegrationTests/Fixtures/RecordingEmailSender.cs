using System.Collections.Concurrent;
using Needlr.Application.Abstractions;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Test double for <see cref="IEmailSender"/>. Records every call so tests can assert that
/// dispatch happened (or didn't, when prefs say so).
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    public ConcurrentBag<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

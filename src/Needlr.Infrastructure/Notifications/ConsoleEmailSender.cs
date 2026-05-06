using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Dev / fallback email sender — writes to <see cref="ILogger"/> at Information level so
/// developers can see what would have shipped without configuring SendGrid. Used whenever
/// <c>NotificationsOptions.SendGridApiKey</c> is null/empty.
/// </summary>
internal sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email dispatch (console fallback) | to={To} subject=\"{Subject}\" body=\"{Body}\"",
            to, subject, body);
        return Task.CompletedTask;
    }
}

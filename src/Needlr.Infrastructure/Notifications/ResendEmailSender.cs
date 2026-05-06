using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Production email sender backed by Resend's REST API. Wired by <c>DependencyInjection</c>
/// via <c>AddHttpClient&lt;IEmailSender, ResendEmailSender&gt;</c> when
/// <see cref="NotificationsOptions.ResendApiKey"/> is set; without a key the dispatcher
/// uses <see cref="ConsoleEmailSender"/> instead. POST <c>https://api.resend.com/emails</c>
/// with a bearer token; per-channel failures are logged but not propagated by the
/// dispatcher, so a transient Resend hiccup never breaks the calling handler.
/// </summary>
internal sealed class ResendEmailSender(
    HttpClient http,
    IOptions<NotificationsOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ResendApiKey))
            throw new InvalidOperationException("ResendApiKey is required to send via Resend.");

        // The Authorization header is set per-call rather than on the HttpClient default
        // headers because the bound options instance can be re-resolved if config reloads.
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new ResendSendRequest(
                From: $"{opts.FromName} <{opts.FromEmail}>",
                To: [to],
                Subject: subject,
                Text: body)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ResendApiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning(
            "Resend send failed | status={Status} to={To} body={Body}",
            (int)response.StatusCode, to, errorBody);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ResendSendRequest(string From, string[] To, string Subject, string Text);
}

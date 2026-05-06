using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Production email sender backed by SendGrid's v3 REST API. Wired by
/// <c>DependencyInjection</c> via <c>AddHttpClient&lt;IEmailSender, SendGridEmailSender&gt;</c>
/// when <see cref="NotificationsOptions.SendGridApiKey"/> is set; without a key the
/// dispatcher uses <see cref="ConsoleEmailSender"/> instead. POST
/// <c>https://api.sendgrid.com/v3/mail/send</c> with a bearer token; per-channel
/// failures are logged but not propagated by the dispatcher, so a transient SendGrid
/// hiccup never breaks the calling handler.
/// </summary>
internal sealed class SendGridEmailSender(
    HttpClient http,
    IOptions<NotificationsOptions> options,
    ILogger<SendGridEmailSender> logger) : IEmailSender
{
    // SendGrid's v3 API expects lowercase property names (personalizations, from,
    // content, etc.) — the default JsonContent.Create uses System.Text.Json defaults
    // which preserve PascalCase, so we pass camelCase Web options explicitly.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.SendGridApiKey))
            throw new InvalidOperationException("SendGridApiKey is required to send via SendGrid.");

        // The Authorization header is set per-call rather than on the HttpClient default
        // headers because the bound options instance can be re-resolved if config reloads.
        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send")
        {
            Content = JsonContent.Create(
                new SendGridSendRequest(
                    Personalizations: [new SendGridPersonalization([new SendGridAddress(to, null)])],
                    From: new SendGridAddress(opts.FromEmail, opts.FromName),
                    Subject: subject,
                    Content: [new SendGridContent("text/plain", body)]),
                options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.SendGridApiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning(
            "SendGrid send failed | status={Status} to={To} body={Body}",
            (int)response.StatusCode, to, errorBody);
        response.EnsureSuccessStatusCode();
    }

    private sealed record SendGridSendRequest(
        SendGridPersonalization[] Personalizations,
        SendGridAddress From,
        string Subject,
        SendGridContent[] Content);

    private sealed record SendGridPersonalization(SendGridAddress[] To);

    private sealed record SendGridAddress(string Email, string? Name);

    private sealed record SendGridContent(string Type, string Value);
}

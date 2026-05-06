using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Needlr.Web.Services;

/// <summary>
/// Asks the browser to register a Web Push subscription on the first authenticated visit
/// and posts it to <c>POST /api/notifications/push-subscriptions</c>. Skips silently when
/// the browser doesn't support Push (older Safari, dev http origins) or the user denies
/// permission. Pages call <see cref="EnsureSubscribedAsync"/> after successful sign-in.
/// </summary>
public sealed class PushSubscriptionRegistrar
{
    private const string ScriptPath = "./js/pushInterop.js";

    private readonly IJSRuntime _js;
    private readonly AuthState _auth;
    private readonly IHttpClientFactory _http;
    private bool _subscribed;

    public PushSubscriptionRegistrar(IJSRuntime js, AuthState auth, IHttpClientFactory http)
    {
        _js = js;
        _auth = auth;
        _http = http;
    }

    public async Task EnsureSubscribedAsync(string vapidPublicKey, CancellationToken cancellationToken = default)
    {
        if (_subscribed) return;
        if (!_auth.IsAuthenticated) return;
        if (string.IsNullOrEmpty(vapidPublicKey)) return;

        try
        {
            await using var module = await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ScriptPath);
            var sub = await module.InvokeAsync<PushSubscriptionResult?>("subscribe", cancellationToken, vapidPublicKey);
            if (sub is null || string.IsNullOrEmpty(sub.Endpoint)) return;

            var client = _http.CreateClient("NeedlrAuthenticated");
            using var resp = await client.PostAsJsonAsync(
                "api/notifications/push-subscriptions",
                new { endpoint = sub.Endpoint, p256dh = sub.P256dh, auth = sub.Auth },
                cancellationToken);
            resp.EnsureSuccessStatusCode();
            _subscribed = true;
        }
        catch
        {
            // Best-effort. Browser denies / no SW / permission API missing — leave it.
        }
    }

    private sealed record PushSubscriptionResult(string Endpoint, string P256dh, string Auth);
}

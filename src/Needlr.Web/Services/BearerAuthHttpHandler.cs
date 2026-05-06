using System.Net;
using System.Net.Http.Headers;

namespace Needlr.Web.Services;

/// <summary>
/// Delegating handler that injects the current bearer token on every outbound request.
/// Refresh-on-401 is handled in <see cref="NeedlrApiClient"/>; this handler is just the
/// header-injection seam so the typed-client code stays clean.
/// </summary>
internal sealed class BearerAuthHttpHandler : DelegatingHandler
{
    private readonly AuthState _auth;

    public BearerAuthHttpHandler(AuthState auth)
    {
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Hydrate-or-noop. The refresh callback is null here — the API client is the one
        // that knows how to refresh; this handler only sticks an existing token on.
        await _auth.EnsureHydratedAsync(cancellationToken);
        if (_auth.IsAuthenticated)
        {
            // Re-read each call so a refresh elsewhere is picked up immediately.
            var token = await _auth.GetAccessTokenAsync(refresh: null, cancellationToken);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

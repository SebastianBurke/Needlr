using System.Net;
using System.Net.Http.Json;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Client;
using Needlr.Contracts.Common;

namespace Needlr.Web.Services;

/// <summary>
/// HttpClient-backed implementation of <see cref="INeedlrApi"/>. Auth endpoints don't carry
/// a bearer token (the bearer handler tries to attach one, but the controller routes are
/// <c>[AllowAnonymous]</c> so an absent token is fine). Refresh-on-401 is handled here so
/// every method gets the same retry treatment in one place.
/// </summary>
internal sealed class NeedlrApiClient : INeedlrApi
{
    private readonly HttpClient _http;

    public NeedlrApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<LoginRequest, AuthResponse>("api/auth/login", request, cancellationToken);

    public Task<AuthResponse> RegisterCustomerAsync(
        RegisterCustomerRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RegisterCustomerRequest, AuthResponse>(
            "api/auth/register-customer", request, cancellationToken);

    public Task<AuthResponse> RegisterArtistAsync(
        RegisterArtistRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RegisterArtistRequest, AuthResponse>(
            "api/auth/register-artist", request, cancellationToken);

    public Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RefreshTokenRequest, AuthResponse>(
            "api/auth/refresh", request, cancellationToken);

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/logout", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    private async Task<TResponse> PostAndDeserializeAsync<TRequest, TResponse>(
        string path, TRequest request, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(path, request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException(
                (int)response.StatusCode, null, "Empty response body.");
        return body;
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        // Try to parse the API's standard error envelope; fall back to the raw status.
        ApiErrorResponse? apiError = null;
        try
        {
            apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            // Non-JSON body — e.g., 502 from upstream proxy. Fall through.
        }

        var first = apiError?.Errors.Count > 0 ? apiError.Errors[0] : null;
        var message = first?.Message
            ?? $"Request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.";
        throw new NeedlrApiException((int)response.StatusCode, first?.Code, message);
    }
}

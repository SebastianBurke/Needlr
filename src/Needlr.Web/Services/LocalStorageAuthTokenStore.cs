using Microsoft.JSInterop;

namespace Needlr.Web.Services;

/// <summary>
/// Stores the access + refresh tokens (and the basic user identity bits) in
/// <c>localStorage</c> via JS interop. Per ARCHITECTURE.md § Authentication this is
/// acceptable for v1; revisit if XSS posture changes.
/// </summary>
internal sealed class LocalStorageAuthTokenStore(IJSRuntime js) : IAuthTokenStore
{
    private const string AccessTokenKey = "needlr.access_token";
    private const string AccessTokenExpiryKey = "needlr.access_token_expires_at";
    private const string RefreshTokenKey = "needlr.refresh_token";
    private const string RefreshTokenExpiryKey = "needlr.refresh_token_expires_at";
    private const string UserIdKey = "needlr.user_id";
    private const string EmailKey = "needlr.email";
    private const string RoleKey = "needlr.role";

    public async Task<AuthTokens?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var access = await GetAsync(AccessTokenKey, cancellationToken);
        var accessExpiry = await GetAsync(AccessTokenExpiryKey, cancellationToken);
        var refresh = await GetAsync(RefreshTokenKey, cancellationToken);
        var refreshExpiry = await GetAsync(RefreshTokenExpiryKey, cancellationToken);
        if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)
            || !DateTime.TryParse(accessExpiry, out var ax)
            || !DateTime.TryParse(refreshExpiry, out var rx))
            return null;
        return new AuthTokens(access, DateTime.SpecifyKind(ax, DateTimeKind.Utc), refresh,
            DateTime.SpecifyKind(rx, DateTimeKind.Utc));
    }

    public async Task<Guid?> LoadUserIdAsync(CancellationToken cancellationToken = default) =>
        Guid.TryParse(await GetAsync(UserIdKey, cancellationToken), out var id) ? id : null;

    public Task<string?> LoadEmailAsync(CancellationToken cancellationToken = default) =>
        GetAsync(EmailKey, cancellationToken);

    public Task<string?> LoadRoleAsync(CancellationToken cancellationToken = default) =>
        GetAsync(RoleKey, cancellationToken);

    public async Task SaveAsync(
        Guid userId, string email, string role, AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        await SetAsync(AccessTokenKey, tokens.AccessToken, cancellationToken);
        await SetAsync(AccessTokenExpiryKey, tokens.AccessTokenExpiresAtUtc.ToString("O"), cancellationToken);
        await SetAsync(RefreshTokenKey, tokens.RefreshToken, cancellationToken);
        await SetAsync(RefreshTokenExpiryKey, tokens.RefreshTokenExpiresAtUtc.ToString("O"), cancellationToken);
        await SetAsync(UserIdKey, userId.ToString(), cancellationToken);
        await SetAsync(EmailKey, email, cancellationToken);
        await SetAsync(RoleKey, role, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var k in new[] { AccessTokenKey, AccessTokenExpiryKey, RefreshTokenKey,
            RefreshTokenExpiryKey, UserIdKey, EmailKey, RoleKey })
        {
            await js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, k);
        }
    }

    private async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key);
        }
        catch
        {
            // Pre-render or other contexts where JS isn't available — treat as "no token".
            return null;
        }
    }

    private async Task SetAsync(string key, string value, CancellationToken cancellationToken) =>
        await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, value);
}

using Needlr.Contracts.Auth;

namespace Needlr.Web.Services;

/// <summary>
/// Scoped (= circuit-lifetime in WebAssembly) auth state. Owns the in-memory token snapshot,
/// fan-outs change events to subscribers (nav, page guards), and bridges to the
/// <see cref="IAuthTokenStore"/> for cross-refresh persistence. Pages and components read
/// <see cref="IsAuthenticated"/> + <see cref="Role"/>; the bearer-handler reads
/// <see cref="GetAccessTokenAsync"/>.
/// </summary>
public sealed class AuthState
{
    private readonly IAuthTokenStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _hydrated;
    private AuthTokens? _tokens;
    private Guid? _userId;
    private string? _email;
    private string? _role;

    public AuthState(IAuthTokenStore store)
    {
        _store = store;
    }

    public event Action? Changed;

    public Guid? UserId => _userId;
    public string? Email => _email;
    public string? Role => _role;
    public bool IsAuthenticated => _tokens is not null;

    /// <summary>
    /// Reads the bearer token, refreshing it (lazily) when the access token has expired
    /// but the refresh token is still valid. Returns null when not signed in or when the
    /// session has fully expired.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(
        Func<RefreshTokenRequest, CancellationToken, Task<AuthResponse>>? refresh,
        CancellationToken cancellationToken = default)
    {
        await EnsureHydratedAsync(cancellationToken);

        if (_tokens is null) return null;
        // 30s leeway so we don't race past expiry on slow networks.
        if (_tokens.AccessTokenExpiresAtUtc > DateTime.UtcNow.AddSeconds(30))
            return _tokens.AccessToken;

        if (refresh is null) return _tokens.AccessToken;

        try
        {
            await _gate.WaitAsync(cancellationToken);
            if (_tokens is null) return null;
            if (_tokens.AccessTokenExpiresAtUtc > DateTime.UtcNow.AddSeconds(30))
                return _tokens.AccessToken;

            var resp = await refresh(new RefreshTokenRequest(_tokens.RefreshToken), cancellationToken);
            await SignInAsync(resp, cancellationToken);
            return resp.AccessToken;
        }
        catch
        {
            // Refresh failed (rotated/revoked); drop the session.
            await SignOutAsync(cancellationToken);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SignInAsync(AuthResponse response, CancellationToken cancellationToken = default)
    {
        _tokens = new AuthTokens(
            response.AccessToken, response.AccessTokenExpiresAtUtc,
            response.RefreshToken, response.RefreshTokenExpiresAtUtc);
        _userId = response.UserId;
        _email = response.Email;
        _role = response.Role;
        _hydrated = true;
        await _store.SaveAsync(response.UserId, response.Email, response.Role, _tokens, cancellationToken);
        Changed?.Invoke();
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        _tokens = null;
        _userId = null;
        _email = null;
        _role = null;
        await _store.ClearAsync(cancellationToken);
        Changed?.Invoke();
    }

    /// <summary>
    /// Lazy hydrate from <see cref="IAuthTokenStore"/>. Triggered on first
    /// <see cref="GetAccessTokenAsync"/> call so we don't pay JS interop on the first paint
    /// of pages that don't need auth.
    /// </summary>
    public async Task EnsureHydratedAsync(CancellationToken cancellationToken = default)
    {
        if (_hydrated) return;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_hydrated) return;
            _tokens = await _store.LoadAsync(cancellationToken);
            _userId = await _store.LoadUserIdAsync(cancellationToken);
            _email = await _store.LoadEmailAsync(cancellationToken);
            _role = await _store.LoadRoleAsync(cancellationToken);
            _hydrated = true;
            if (_tokens is not null) Changed?.Invoke();
        }
        finally
        {
            _gate.Release();
        }
    }
}

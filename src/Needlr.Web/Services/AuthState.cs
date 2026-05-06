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

    /// <summary>
    /// Fires specifically on the authenticated → not-authenticated transition (sign-out
    /// button, refresh-token failure). Distinct from <see cref="Changed"/> so consumers
    /// (nav, page guards) can react to a logout without having to diff state themselves.
    /// </summary>
    public event Action? SignedOut;

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
            // Refresh failed (rotated/revoked); drop the session. No backend logout since
            // the refresh attempt itself just failed against the server.
            await SignOutAsync(backendLogout: null, cancellationToken);
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

    /// <summary>
    /// Clears local auth state. Pass <paramref name="backendLogout"/> to also revoke the
    /// refresh token server-side (best-effort — failures don't block the local clear, since
    /// the user expects to be logged out regardless of network state). The delegate is
    /// passed in rather than injected to keep AuthState free of <c>INeedlrApi</c> (which
    /// itself depends on AuthState via the bearer-attaching handler).
    /// </summary>
    public async Task SignOutAsync(
        Func<LogoutRequest, CancellationToken, Task>? backendLogout = null,
        CancellationToken cancellationToken = default)
    {
        var wasAuthed = _tokens is not null;
        var refresh = _tokens?.RefreshToken;
        if (backendLogout is not null && refresh is not null)
        {
            try { await backendLogout(new LogoutRequest(refresh), cancellationToken); }
            catch { /* best-effort; the local clear below still happens */ }
        }
        _tokens = null;
        _userId = null;
        _email = null;
        _role = null;
        await _store.ClearAsync(cancellationToken);
        Changed?.Invoke();
        if (wasAuthed) SignedOut?.Invoke();
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

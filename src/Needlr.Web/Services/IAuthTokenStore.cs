namespace Needlr.Web.Services;

/// <summary>
/// Persists the auth-token snapshot across page refreshes. Implementation uses
/// <c>localStorage</c> via JS interop; pages never talk to it directly — they go through
/// <see cref="AuthState"/>.
/// </summary>
public interface IAuthTokenStore
{
    Task<AuthTokens?> LoadAsync(CancellationToken cancellationToken = default);
    Task<Guid?> LoadUserIdAsync(CancellationToken cancellationToken = default);
    Task<string?> LoadEmailAsync(CancellationToken cancellationToken = default);
    Task<string?> LoadRoleAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        Guid userId, string email, string role,
        AuthTokens tokens, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

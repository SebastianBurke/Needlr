namespace Needlr.Application.Abstractions;

/// <summary>
/// Persistence and rotation for refresh tokens. The <em>raw</em> token is returned to the client
/// once on issue/rotate; only its SHA256 hash is stored. <see cref="RotateAsync"/> revokes the
/// presented token in the same operation that issues a new one — re-presenting an already-rotated
/// token is detectable by callers.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>Issues a new refresh token for <paramref name="userId"/>.</summary>
    Task<RefreshTokenIssued> IssueAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates <paramref name="presentedToken"/>, marks it revoked, and returns a freshly-issued
    /// replacement bound to the same user. Returns <c>null</c> if the token is unknown, expired,
    /// or already revoked.
    /// </summary>
    Task<RefreshTokenRotated?> RotateAsync(string presentedToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the token as revoked. No-op if the token is unknown — logout endpoints should not
    /// leak whether a token was valid.
    /// </summary>
    Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenIssued(string Token, DateTime ExpiresAtUtc);

public sealed record RefreshTokenRotated(Guid UserId, string Token, DateTime ExpiresAtUtc);

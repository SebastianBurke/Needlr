namespace Needlr.Infrastructure.Identity;

/// <summary>
/// Persisted refresh token for the JWT auth flow. The raw token is sent to the client; only
/// a SHA256 hash is stored here. Tokens rotate on every refresh — the previous row is marked
/// <see cref="IsRevoked"/> and points to its replacement via <see cref="ReplacedByTokenId"/>
/// so we can detect re-use of an already-rotated token (a sign of theft).
/// </summary>
public sealed class RefreshToken
{
    public const int TokenHashLength = 64;  // SHA256 hex

    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTime nowUtc) => !IsRevoked && nowUtc < ExpiresAt;
}

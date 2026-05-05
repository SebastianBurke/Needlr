using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions;

/// <summary>
/// Issues short-lived JWT access tokens. Refresh tokens are managed separately by
/// <see cref="IRefreshTokenStore"/> — keeping the two concerns split lets the access-token
/// signing key rotate independently of refresh-token storage.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Issues a JWT for the given identity. The token carries <c>sub</c>, <c>email</c>,
    /// and <c>role</c> claims; the lifetime is configured via <c>JwtOptions</c>.
    /// </summary>
    JwtAccessToken Issue(Guid userId, string email, UserRole role);
}

public sealed record JwtAccessToken(string Token, DateTime ExpiresAtUtc);

using Needlr.Domain.Enums;

namespace Needlr.Application.Auth;

/// <summary>
/// Successful outcome of any auth operation that issues tokens (register, login, refresh).
/// The Api layer maps this to <c>Needlr.Contracts.Auth.AuthResponse</c> for the HTTP boundary.
/// </summary>
public sealed record AuthResult(
    Guid UserId,
    string Email,
    UserRole Role,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

namespace Needlr.Contracts.Auth;

/// <summary>
/// Successful response from any auth endpoint that issues tokens (register, login, refresh).
/// <see cref="Role"/> is the string form of <c>Needlr.Domain.Enums.UserRole</c>; the client
/// uses it to drive role-conditional UI.
/// </summary>
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string Role,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

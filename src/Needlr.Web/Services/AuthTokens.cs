namespace Needlr.Web.Services;

/// <summary>
/// In-memory snapshot of the auth tokens we hold for the currently signed-in user. Mirrors
/// the wire-format <c>AuthResponse</c> minus user id / email / role (those live on
/// <see cref="AuthState"/> as observable fields).
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

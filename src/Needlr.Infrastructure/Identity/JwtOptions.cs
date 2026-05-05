using System.ComponentModel.DataAnnotations;

namespace Needlr.Infrastructure.Identity;

/// <summary>
/// Bound from configuration section "Jwt". Symmetric HMAC-SHA256 signing — <see cref="SigningKey"/>
/// must be at least 32 bytes (UTF-8). Production sets the key via the <c>Jwt__SigningKey</c>
/// environment variable; <c>appsettings.Development.json</c> carries a dev-only key.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = null!;
    [Required] public string Audience { get; set; } = null!;
    [Required, MinLength(32)] public string SigningKey { get; set; } = null!;

    [Range(1, 1440)] public int AccessTokenLifetimeMinutes { get; set; } = 15;
    [Range(1, 365)] public int RefreshTokenLifetimeDays { get; set; } = 30;
}

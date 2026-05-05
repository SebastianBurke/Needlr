using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Identity;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly IClock _clock = clock;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtAccessToken Issue(Guid userId, string email, UserRole role)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            // ClaimTypes.NameIdentifier / Email / Role mirrors what JwtBearer maps the JWT claims
            // into for the API's HttpContext.User principal — including them lets handlers read
            // the standard ClaimTypes.* constants without per-claim mapping setup on the bearer
            // scheme.
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtAccessToken(_handler.WriteToken(token), expires);
    }
}

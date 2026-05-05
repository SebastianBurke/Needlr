using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Identity;

internal sealed class RefreshTokenStore(
    NeedlrDbContext db,
    IClock clock,
    IOptions<JwtOptions> options) : IRefreshTokenStore
{
    private readonly NeedlrDbContext _db = db;
    private readonly IClock _clock = clock;
    private readonly JwtOptions _options = options.Value;

    public async Task<RefreshTokenIssued> IssueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateRawToken();
        var record = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Sha256Hex(rawToken),
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)
        };

        _db.RefreshTokens.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenIssued(rawToken, record.ExpiresAt);
    }

    public async Task<RefreshTokenRotated?> RotateAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hex(presentedToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);
        if (existing is null || !existing.IsActive(_clock.UtcNow))
            return null;

        // Revoke the presented token in the same transaction it issues a new one. A subsequent
        // re-presentation of the now-revoked token returns null, which the caller surfaces as
        // an Unauthorized — re-use of a rotated token is a theft signal.
        existing.IsRevoked = true;
        existing.RevokedAt = _clock.UtcNow;

        var rawToken = GenerateRawToken();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = Sha256Hex(rawToken),
            CreatedAt = _clock.UtcNow,
            ExpiresAt = _clock.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)
        };

        _db.RefreshTokens.Add(replacement);
        existing.ReplacedByTokenId = replacement.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotated(existing.UserId, rawToken, replacement.ExpiresAt);
    }

    public async Task RevokeAsync(string presentedToken, CancellationToken cancellationToken = default)
    {
        var hash = Sha256Hex(presentedToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);
        if (existing is null || existing.IsRevoked)
            return;

        existing.IsRevoked = true;
        existing.RevokedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Sha256Hex(string token)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

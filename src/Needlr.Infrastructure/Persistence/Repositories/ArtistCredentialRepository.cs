using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class ArtistCredentialRepository(NeedlrDbContext db) : IArtistCredentialRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<ArtistCredential?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken = default) =>
        _db.ArtistCredentials.FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken);

    public async Task<IReadOnlyList<ArtistCredential>> ListByStatusAsync(
        VerificationStatus status, CancellationToken cancellationToken = default) =>
        await _db.ArtistCredentials
            .Where(c => c.VerificationStatus == status)
            .OrderBy(c => c.IssuedDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ArtistCredential>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        await _db.ArtistCredentials
            .Where(c => c.ArtistId == artistId)
            .ToListAsync(cancellationToken);

    public void Add(ArtistCredential credential) => _db.ArtistCredentials.Add(credential);
}

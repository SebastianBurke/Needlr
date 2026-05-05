using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class StudioCredentialRepository(NeedlrDbContext db) : IStudioCredentialRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<StudioCredential?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken = default) =>
        _db.StudioCredentials.FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken);

    public async Task<IReadOnlyList<StudioCredential>> ListByStatusAsync(
        VerificationStatus status, CancellationToken cancellationToken = default) =>
        await _db.StudioCredentials
            .Where(c => c.VerificationStatus == status)
            .OrderBy(c => c.IssuedDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StudioCredential>> ListByStudioAsync(
        Guid studioId, CancellationToken cancellationToken = default) =>
        await _db.StudioCredentials
            .Where(c => c.StudioId == studioId)
            .ToListAsync(cancellationToken);

    public void Add(StudioCredential credential) => _db.StudioCredentials.Add(credential);
}

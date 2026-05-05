using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class ArtistStudioAffiliationRepository(NeedlrDbContext db) : IArtistStudioAffiliationRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<ArtistStudioAffiliation?> GetByIdAsync(Guid affiliationId, CancellationToken cancellationToken = default) =>
        _db.ArtistStudioAffiliations.FirstOrDefaultAsync(a => a.Id == affiliationId, cancellationToken);

    public Task<ArtistStudioAffiliation?> GetByArtistAndStudioAsync(
        Guid artistId, Guid studioId, CancellationToken cancellationToken = default) =>
        _db.ArtistStudioAffiliations
            .FirstOrDefaultAsync(a => a.ArtistId == artistId && a.StudioId == studioId, cancellationToken);

    public async Task<IReadOnlyList<ArtistStudioAffiliation>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        await _db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId)
            .OrderBy(a => a.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ArtistStudioAffiliation>> ListByStudioAsync(
        Guid studioId, AffiliationStatus? status = null, CancellationToken cancellationToken = default)
    {
        var q = _db.ArtistStudioAffiliations.AsQueryable().Where(a => a.StudioId == studioId);
        if (status is { } s) q = q.Where(a => a.Status == s);
        return await q.OrderBy(a => a.StartDate).ToListAsync(cancellationToken);
    }

    public void Add(ArtistStudioAffiliation affiliation) => _db.ArtistStudioAffiliations.Add(affiliation);

    public void Remove(ArtistStudioAffiliation affiliation) => _db.ArtistStudioAffiliations.Remove(affiliation);

    public Task<bool> IsAdminAsync(Guid artistId, Guid studioId, CancellationToken cancellationToken = default) =>
        _db.ArtistStudioAffiliations.AnyAsync(
            a => a.ArtistId == artistId
                && a.StudioId == studioId
                && a.Status == AffiliationStatus.Active
                && (a.Role == AffiliationRole.Admin || a.Role == AffiliationRole.Founder),
            cancellationToken);
}

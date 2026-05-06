using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Availability;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class ArtistAvailabilityProjectionRepository(NeedlrDbContext db)
    : IArtistAvailabilityProjectionRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<ArtistAvailabilityProjection>> ListAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await _db.ArtistAvailabilityProjections
            .Where(p => p.ArtistId == artistId && p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);

    public async Task DeleteWindowAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync issues a single DELETE WHERE which is what we want — bulk-deleting
        // tracked entities one at a time wastes round-trips and risks inserting then deleting in
        // the same SaveChanges if the caller adds new rows for the same window.
        await _db.ArtistAvailabilityProjections
            .Where(p => p.ArtistId == artistId && p.Date >= from && p.Date <= to)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(ArtistAvailabilityProjection projection) =>
        _db.ArtistAvailabilityProjections.Add(projection);
}

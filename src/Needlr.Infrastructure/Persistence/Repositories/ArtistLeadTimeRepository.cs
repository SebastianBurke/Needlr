using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class ArtistLeadTimeRepository(NeedlrDbContext db) : IArtistLeadTimeRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<ArtistLeadTime>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        await _db.ArtistLeadTimes
            .Where(lt => lt.ArtistId == artistId)
            .OrderBy(lt => lt.BookingType)
            .ToListAsync(cancellationToken);

    public void Add(ArtistLeadTime leadTime) => _db.ArtistLeadTimes.Add(leadTime);
    public void Remove(ArtistLeadTime leadTime) => _db.ArtistLeadTimes.Remove(leadTime);
}

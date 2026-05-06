using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Availability;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class AvailabilityOverrideRepository(NeedlrDbContext db) : IAvailabilityOverrideRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<AvailabilityOverride?> GetAsync(
        Guid artistId, DateOnly date, CancellationToken cancellationToken = default) =>
        _db.AvailabilityOverrides
            .FirstOrDefaultAsync(o => o.ArtistId == artistId && o.Date == date, cancellationToken);

    public async Task<IReadOnlyList<AvailabilityOverride>> ListByArtistAsync(
        Guid artistId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var q = _db.AvailabilityOverrides.Where(o => o.ArtistId == artistId);
        if (from is { } f) q = q.Where(o => o.Date >= f);
        if (to is { } t) q = q.Where(o => o.Date <= t);
        return await q.OrderBy(o => o.Date).ToListAsync(cancellationToken);
    }

    public void Add(AvailabilityOverride availabilityOverride) =>
        _db.AvailabilityOverrides.Add(availabilityOverride);

    public void Remove(AvailabilityOverride availabilityOverride) =>
        _db.AvailabilityOverrides.Remove(availabilityOverride);
}

using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Availability;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class AvailabilityPatternRepository(NeedlrDbContext db) : IAvailabilityPatternRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<AvailabilityPattern>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        await _db.AvailabilityPatterns
            .Where(p => p.ArtistId == artistId)
            .OrderBy(p => p.DayOfWeek).ThenBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public void Add(AvailabilityPattern pattern) => _db.AvailabilityPatterns.Add(pattern);
    public void Remove(AvailabilityPattern pattern) => _db.AvailabilityPatterns.Remove(pattern);
    public void RemoveRange(IEnumerable<AvailabilityPattern> patterns) =>
        _db.AvailabilityPatterns.RemoveRange(patterns);
}

using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Availability;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class BookingWindowRepository(NeedlrDbContext db) : IBookingWindowRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<BookingWindow?> GetByIdAsync(Guid windowId, CancellationToken cancellationToken = default) =>
        _db.BookingWindows.FirstOrDefaultAsync(w => w.Id == windowId, cancellationToken);

    public async Task<IReadOnlyList<BookingWindow>> ListByArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        await _db.BookingWindows
            .Where(w => w.ArtistId == artistId)
            .OrderBy(w => w.WindowOpensAt)
            .ToListAsync(cancellationToken);

    public void Add(BookingWindow window) => _db.BookingWindows.Add(window);
    public void Remove(BookingWindow window) => _db.BookingWindows.Remove(window);
}

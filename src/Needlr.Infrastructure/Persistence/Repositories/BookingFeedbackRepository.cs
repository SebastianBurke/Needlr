using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Bookings;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class BookingFeedbackRepository(NeedlrDbContext db) : IBookingFeedbackRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<BookingFeedback?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _db.BookingFeedbacks.FirstOrDefaultAsync(f => f.BookingId == bookingId, cancellationToken);

    public async Task<IReadOnlyList<BookingFeedback>> ListRecentForArtistAsync(
        Guid artistId, int take, CancellationToken cancellationToken = default) =>
        await _db.BookingFeedbacks
            .Where(f => _db.Bookings.Any(b => b.Id == f.BookingId && b.ArtistId == artistId))
            .OrderByDescending(f => f.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(BookingFeedback feedback) => _db.BookingFeedbacks.Add(feedback);
}

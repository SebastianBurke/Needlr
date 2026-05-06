using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class MessageThreadRepository(NeedlrDbContext db) : IMessageThreadRepository
{
    private static readonly BookingStatus[] TerminalStatuses =
    [
        BookingStatus.Completed,
        BookingStatus.CancelledByArtist,
        BookingStatus.CancelledByCustomer,
        BookingStatus.Declined,
        BookingStatus.Expired
    ];

    private readonly NeedlrDbContext _db = db;

    public Task<MessageThread?> GetByIdAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        _db.MessageThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

    public Task<MessageThread?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _db.MessageThreads.FirstOrDefaultAsync(t => t.BookingId == bookingId, cancellationToken);

    public async Task<(MessageThread Thread, Booking Booking)?> GetWithBookingAsync(
        Guid threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _db.MessageThreads.FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);
        if (thread is null) return null;
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == thread.BookingId, cancellationToken);
        if (booking is null) return null;
        return (thread, booking);
    }

    public async Task<PagedResult<MessageThread>> ListActiveForUserAsync(
        Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();

        // A user is a party iff they're the booking's customer OR they're the artist's
        // ApplicationUser. Pull artistId via subquery so EF translates to a single SQL.
        var threads = _db.MessageThreads
            .Where(t => t.Status == MessageThreadStatus.Active
                && _db.Bookings.Any(b => b.Id == t.BookingId
                    && (b.CustomerId == userId
                        || _db.Artists.Any(a => a.Id == b.ArtistId && a.UserId == userId))));

        var total = await threads.CountAsync(cancellationToken);
        var rows = await threads
            // Latest message first (or OpenedAt if no messages yet).
            .OrderByDescending(t => _db.Messages
                .Where(m => m.ThreadId == t.Id)
                .Max(m => (DateTime?)m.SentAt) ?? t.OpenedAt)
            .Skip(p.Skip).Take(p.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<MessageThread>(rows, p.Page, p.PageSize, total);
    }

    public async Task<IReadOnlyList<MessageThread>> ListLockableAsync(
        DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        await _db.MessageThreads
            .Where(t => t.Status == MessageThreadStatus.Active
                && _db.Bookings.Any(b => b.Id == t.BookingId
                    && TerminalStatuses.Contains(b.Status)
                    && (b.CompletedAt ?? b.RequestedAt) <= cutoffUtc))
            .ToListAsync(cancellationToken);

    public void Add(MessageThread thread) => _db.MessageThreads.Add(thread);
}

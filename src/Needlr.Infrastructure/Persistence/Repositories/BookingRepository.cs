using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class BookingRepository(NeedlrDbContext db) : IBookingRepository
{
    // Statuses that consume capacity in the availability projector. Cancelled/Declined/Expired
    // never enter this set; Completed is excluded because completed sessions are in the past
    // and the projector is forward-looking.
    private static readonly BookingStatus[] CapacityConsumingStatuses =
    [
        BookingStatus.Accepted,
        BookingStatus.DepositCaptured,
        BookingStatus.Confirmed,
        BookingStatus.InProgress
    ];

    // Statuses that should appear on the iCal feed. Includes Completed so past sessions
    // remain visible to subscribed calendar clients.
    private static readonly BookingStatus[] FeedVisibleStatuses =
    [
        BookingStatus.Accepted,
        BookingStatus.DepositCaptured,
        BookingStatus.Confirmed,
        BookingStatus.InProgress,
        BookingStatus.Completed
    ];

    private readonly NeedlrDbContext _db = db;

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public Task<Booking?> GetByIdForCustomerAsync(
        Guid bookingId, Guid customerId, CancellationToken cancellationToken = default) =>
        _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId && b.CustomerId == customerId, cancellationToken);

    public Task<Booking?> GetByIdForArtistAsync(
        Guid bookingId, Guid artistId, CancellationToken cancellationToken = default) =>
        _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId && b.ArtistId == artistId, cancellationToken);

    public Task<BookingWithNames?> GetByIdWithNamesAsync(
        Guid bookingId, CancellationToken cancellationToken = default) =>
        // Detail surface: the embedded ThreadView marks unread messages read on render, so a
        // per-booking unread count would always race itself to zero. Pass Guid.Empty to keep
        // the projection shape uniform; no real user has that id, so the unread count subquery
        // matches no rows and naturally returns 0.
        ProjectWithNames(_db.Bookings.Where(b => b.Id == bookingId), Guid.Empty)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Booking>> ListForCustomerAsync(
        Guid customerId, BookingStatus? status, PageRequest page, CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();
        var q = _db.Bookings.Where(b => b.CustomerId == customerId);
        if (status is { } s) q = q.Where(b => b.Status == s);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(b => b.RequestedAt)
            .Skip(p.Skip).Take(p.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Booking>(items, p.Page, p.PageSize, total);
    }

    public async Task<PagedResult<Booking>> ListForArtistAsync(
        Guid artistId, BookingStatus? status, PageRequest page, CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();
        var q = _db.Bookings.Where(b => b.ArtistId == artistId);
        if (status is { } s) q = q.Where(b => b.Status == s);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(b => b.RequestedAt)
            .Skip(p.Skip).Take(p.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Booking>(items, p.Page, p.PageSize, total);
    }

    public Task<PagedResult<BookingWithNames>> ListForCustomerWithNamesAsync(
        Guid customerId, Guid requestingUserId, BookingStatus? status,
        PageRequest page, CancellationToken cancellationToken = default)
    {
        var q = _db.Bookings.Where(b => b.CustomerId == customerId);
        if (status is { } s) q = q.Where(b => b.Status == s);
        return PageProjectedAsync(q, requestingUserId, page, cancellationToken);
    }

    public Task<PagedResult<BookingWithNames>> ListForArtistWithNamesAsync(
        Guid artistId, Guid requestingUserId, BookingStatus? status,
        PageRequest page, CancellationToken cancellationToken = default)
    {
        var q = _db.Bookings.Where(b => b.ArtistId == artistId);
        if (status is { } s) q = q.Where(b => b.Status == s);
        return PageProjectedAsync(q, requestingUserId, page, cancellationToken);
    }

    // CustomerId on a Booking is the auth user id (mirrors RequestBookingCommandHandler.cs);
    // CustomerProfile keys off the same UserId. Left-joins protect us if a profile is missing
    // — the booking still renders with an "Unknown" placeholder rather than a 500.
    // Filtering happens on the Booking source before projection so EF can translate the WHERE
    // clauses to SQL (filtering after the projection uses the BookingWithNames constructor in
    // the query tree, which the Npgsql translator rejects).
    //
    // Unread count mirrors IMessageRepository.CountUnreadForUserAsync's predicate scoped to the
    // single booking row: only Active threads, only messages the requesting user didn't author,
    // only messages they haven't read. Pass Guid.Empty for surfaces that don't need the count
    // (the SenderId != Guid.Empty filter still excludes nothing in practice but the wider
    // query is gated by an Active-thread match for that specific booking).
    private IQueryable<BookingWithNames> ProjectWithNames(
        IQueryable<Booking> source, Guid requestingUserId) =>
        from b in source
        join a in _db.Artists on b.ArtistId equals a.Id
        join cp in _db.CustomerProfiles on b.CustomerId equals cp.UserId into cpj
        from cp in cpj.DefaultIfEmpty()
        select new BookingWithNames(
            b,
            cp != null ? cp.DisplayName : "Unknown customer",
            a.DisplayName,
            _db.Messages.Count(m =>
                m.ReadAt == null
                && m.SenderId != requestingUserId
                && _db.MessageThreads.Any(t =>
                    t.Id == m.ThreadId
                    && t.BookingId == b.Id
                    && t.Status == Domain.Enums.MessageThreadStatus.Active)));

    private async Task<PagedResult<BookingWithNames>> PageProjectedAsync(
        IQueryable<Booking> filtered, Guid requestingUserId,
        PageRequest page, CancellationToken cancellationToken)
    {
        var p = page.Clamp();
        var total = await filtered.CountAsync(cancellationToken);
        var items = await ProjectWithNames(
                filtered.OrderByDescending(b => b.RequestedAt).Skip(p.Skip).Take(p.PageSize),
                requestingUserId)
            .ToListAsync(cancellationToken);
        return new PagedResult<BookingWithNames>(items, p.Page, p.PageSize, total);
    }

    public async Task<IReadOnlyList<Booking>> ListRequestedExpiredAsync(
        DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
        await _db.Bookings
            .Where(b => b.Status == BookingStatus.Requested && b.RequestedAt <= cutoffUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListConsumingForArtistInWindowAsync(
        Guid artistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return await _db.Bookings
            .Where(b => b.ArtistId == artistId
                && b.ConfirmedSessionDate != null
                && b.ConfirmedSessionDate >= fromUtc
                && b.ConfirmedSessionDate <= toUtc
                && CapacityConsumingStatuses.Contains(b.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListConfirmedForArtistFromAsync(
        Guid artistId, DateTime from, CancellationToken cancellationToken = default) =>
        await _db.Bookings
            .Where(b => b.ArtistId == artistId
                && b.ConfirmedSessionDate != null
                && b.ConfirmedSessionDate >= from
                && FeedVisibleStatuses.Contains(b.Status))
            .OrderBy(b => b.ConfirmedSessionDate)
            .ToListAsync(cancellationToken);

    public void Add(Booking booking) => _db.Bookings.Add(booking);
}

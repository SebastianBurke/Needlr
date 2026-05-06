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

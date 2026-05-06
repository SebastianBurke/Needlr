using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
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

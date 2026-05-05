using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Bookings;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class BookingRepository(NeedlrDbContext db) : IBookingRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public Task<Booking?> GetByIdForCustomerAsync(
        Guid bookingId, Guid customerId, CancellationToken cancellationToken = default) =>
        _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId && b.CustomerId == customerId, cancellationToken);

    public void Add(Booking booking) => _db.Bookings.Add(booking);
}

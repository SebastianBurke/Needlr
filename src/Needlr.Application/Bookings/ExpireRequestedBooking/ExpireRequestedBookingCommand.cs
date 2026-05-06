using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.ExpireRequestedBooking;

/// <summary>
/// Single-booking expiry. Idempotent: a booking already past <c>Requested</c> resolves
/// without changing state. Used by the Hangfire job
/// <c>ExpireDueRequestedBookingsRecurringJob</c> and as an admin/manual trigger.
/// </summary>
public sealed record ExpireRequestedBookingCommand(Guid BookingId) : ICommand;

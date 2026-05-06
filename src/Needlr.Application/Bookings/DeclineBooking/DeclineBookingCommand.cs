using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.DeclineBooking;

public sealed record DeclineBookingCommand(
    Guid BookingId,
    DeclineReason Reason,
    string? Note) : ICommand;

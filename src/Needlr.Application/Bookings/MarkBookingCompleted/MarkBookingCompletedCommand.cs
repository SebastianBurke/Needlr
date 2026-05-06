using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.MarkBookingCompleted;

public sealed record MarkBookingCompletedCommand(Guid BookingId) : ICommand;

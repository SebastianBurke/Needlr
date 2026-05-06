using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.MarkBookingInProgress;

public sealed record MarkBookingInProgressCommand(Guid BookingId) : ICommand;

using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.AcceptBooking;

/// <summary>
/// Artist accepts a Requested booking and confirms a session date/time. Phase 10 stops at
/// status = <c>Accepted</c>; Phase 11 chains the Stripe capture which transitions the
/// booking through <c>DepositCaptured</c> → <c>Confirmed</c> on webhook receipt. Triggers
/// an availability projector rebuild because the now-confirmed session consumes capacity.
/// </summary>
public sealed record AcceptBookingCommand(
    Guid BookingId,
    DateTime ConfirmedSessionDateUtc) : ICommand;

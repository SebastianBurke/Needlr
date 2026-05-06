using Needlr.Application.Messaging;

namespace Needlr.Application.Artists.SetAcceptingBookings;

/// <summary>
/// Sets the calling artist's accepting-new-bookings flag. When false, the artist still
/// appears in discovery + studio rosters, but their profile shows a "not taking bookings"
/// indicator and the booking-request action is hidden.
/// </summary>
public sealed record SetAcceptingBookingsCommand(bool Accepting) : ICommand;

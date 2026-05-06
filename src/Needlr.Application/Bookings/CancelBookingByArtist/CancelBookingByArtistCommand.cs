using Needlr.Application.Bookings.CancelBookingByCustomer;
using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.CancelBookingByArtist;

public sealed record CancelBookingByArtistCommand(Guid BookingId) : ICommand<CancelBookingResult>;

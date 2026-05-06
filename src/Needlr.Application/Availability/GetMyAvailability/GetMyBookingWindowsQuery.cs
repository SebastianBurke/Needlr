using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.GetMyAvailability;

public sealed record GetMyBookingWindowsQuery : IQuery<IReadOnlyList<BookingWindowDto>>;

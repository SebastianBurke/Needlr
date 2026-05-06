using MediatR;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.SetAcceptingBookings;

/// <summary>
/// Reads the calling artist's accepting-new-bookings flag for the settings form.
/// </summary>
public sealed record GetMyAcceptingBookingsQuery : IRequest<Result<bool>>;

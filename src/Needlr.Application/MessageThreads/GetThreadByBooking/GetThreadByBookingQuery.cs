using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.GetThreadByBooking;

/// <summary>
/// Resolve the booking-scoped message thread (if any) for a given booking. Replaces the
/// list-and-filter pattern in BookingDetail/ThreadView that pulled <c>/api/threads/mine</c>
/// then filtered client-side by booking id — that path silently capped at 100 active
/// threads per call and made the FE walk a list to find one row.
/// </summary>
public sealed record GetThreadByBookingQuery(Guid BookingId) : IQuery<ThreadDto?>;

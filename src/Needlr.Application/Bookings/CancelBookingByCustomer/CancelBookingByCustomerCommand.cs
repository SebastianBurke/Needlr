using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.CancelBookingByCustomer;

public sealed record CancelBookingByCustomerCommand(Guid BookingId) : ICommand<CancelBookingResult>;

/// <summary>
/// Reports the refund decision back to the caller so the FE can show "Your $X was refunded"
/// or "No refund per policy" without re-querying. Phase 11 hooks the actual Stripe refund
/// off this same value.
/// </summary>
public sealed record CancelBookingResult(decimal RefundedAmountCad);

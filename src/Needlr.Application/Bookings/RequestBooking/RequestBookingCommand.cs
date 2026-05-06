using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RequestBooking;

/// <summary>
/// Customer submits a structured booking request. Phase 11 wires Stripe: a manual-capture
/// PaymentIntent pre-authorizes the deposit on the customer's payment method (collected
/// client-side via Stripe Elements) before the booking is persisted. The intent id is
/// stored on the booking; capture happens on artist accept (handled by Stripe webhook
/// flipping status DepositCaptured → Confirmed). Description is run through
/// <c>IContactInfoStripper</c> at write time so the persisted text never contains
/// pre-acceptance backchannel info (FEATURE_SPECS.md § Pre-acceptance content stripping).
/// </summary>
public sealed record RequestBookingCommand(
    Guid ArtistId,
    BookingType BookingType,
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    string Description,
    BodyPlacement BodyPlacement,
    string CustomerPaymentMethodId,
    int? ApproximateSizeCm = null,
    decimal? EstimatedTotalCad = null) : ICommand<Guid>;

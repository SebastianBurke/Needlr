using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RequestBooking;

/// <summary>
/// Customer submits a structured booking request. Phase 10 records the booking in the
/// <c>Requested</c> state with the deposit amount captured for Phase 11 to pre-authorize via
/// Stripe; this handler does NOT call Stripe yet. Description is run through
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
    int? ApproximateSizeCm = null,
    decimal? EstimatedTotalCad = null) : ICommand<Guid>;

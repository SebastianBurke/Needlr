using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RespondWithMoreInfo;

/// <summary>
/// Customer responds with updated info on a booking that the artist sent back as
/// <c>AwaitingCustomerInfo</c>. The customer can revise description, requested date, size,
/// duration, and placement; the booking returns to <c>Requested</c> for the artist to look
/// at again. Description is run through <c>IContactInfoStripper</c> like on initial submit.
/// </summary>
public sealed record RespondWithMoreInfoCommand(
    Guid BookingId,
    string Description,
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    BodyPlacement BodyPlacement,
    int? ApproximateSizeCm = null,
    decimal? EstimatedTotalCad = null) : ICommand;

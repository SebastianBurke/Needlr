using Needlr.Application.Messaging;

namespace Needlr.Application.TrustSafety.SubmitBookingFeedback;

/// <summary>
/// Customer submits private post-booking feedback. Customer-only; one feedback per booking;
/// only valid against Completed bookings; feedback is never shown to artists or other users
/// per ADR-002. Drives the admin trust &amp; safety dashboard.
/// </summary>
public sealed record SubmitBookingFeedbackCommand(
    Guid BookingId,
    int CommunicationRating,
    int CleanlinessRating,
    int RespectedDesignBriefRating,
    bool WouldBookAgain,
    string? FreeText) : ICommand<Guid>;

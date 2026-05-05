namespace Needlr.Domain.Enums;

/// <summary>
/// Lifecycle state of a Booking. Transitions are documented in DOMAIN_MODEL.md § BookingStatus.
/// </summary>
public enum BookingStatus
{
    /// <summary>Customer has submitted a booking request; artist has not yet responded.</summary>
    Requested,

    /// <summary>Artist responded with a structured "request more info" prompt; awaiting customer.</summary>
    AwaitingCustomerInfo,

    /// <summary>Artist clicked Accept; deposit capture in flight.</summary>
    Accepted,

    /// <summary>Stripe confirmed deposit capture.</summary>
    DepositCaptured,

    /// <summary>Stable post-acceptance state. Message thread is open; reminders are scheduled.</summary>
    Confirmed,

    /// <summary>Artist marked the session as in progress (optional; for their own tracking).</summary>
    InProgress,

    /// <summary>Artist marked the session complete. Triggers feedback prompt and healed-photo schedule.</summary>
    Completed,

    /// <summary>Artist cancelled a confirmed booking; full refund per policy.</summary>
    CancelledByArtist,

    /// <summary>Customer cancelled a confirmed booking; refund handled per policy snapshot.</summary>
    CancelledByCustomer,

    /// <summary>Artist declined the request; pre-auth voided.</summary>
    Declined,

    /// <summary>Artist did not respond within 7 days; pre-auth voided automatically.</summary>
    Expired
}

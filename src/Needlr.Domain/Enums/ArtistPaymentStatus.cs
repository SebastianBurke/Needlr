namespace Needlr.Domain.Enums;

/// <summary>
/// Stripe Connect payment status for an artist. Driven by the account.updated webhook.
/// An artist must reach Active before they can accept paid bookings (ADR-005).
/// </summary>
public enum ArtistPaymentStatus
{
    /// <summary>Artist has not yet started Stripe Connect onboarding.</summary>
    NotOnboarded,

    /// <summary>AccountLink generated; awaiting Stripe to confirm details_submitted &amp;&amp; charges_enabled.</summary>
    OnboardingInProgress,

    /// <summary>Stripe reports the connected account can accept charges.</summary>
    Active,

    /// <summary>Stripe placed restrictions on the connected account (compliance, dispute rate, etc.).</summary>
    Restricted
}

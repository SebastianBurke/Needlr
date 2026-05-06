using System.ComponentModel.DataAnnotations;

namespace Needlr.Infrastructure.Stripe;

/// <summary>
/// Bound from the <c>Stripe</c> configuration section. Per ADR-005 the platform never
/// holds funds — these settings are for talking to the Connect API on behalf of artists
/// (account creation + KYC links + per-artist PaymentIntent / Refund calls).
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Platform-level secret API key (sk_test_… in dev, sk_live_… in prod). Per-account
    /// calls inject the artist's connect account id via <c>RequestOptions.StripeAccount</c>.
    /// </summary>
    [Required]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Webhook signing secret for the Connect endpoint subscribed to
    /// <c>account.updated</c> / <c>payment_intent.*</c> / <c>charge.*</c>. Verified via
    /// <c>EventUtility.ConstructEvent</c> on every inbound webhook.
    /// </summary>
    [Required]
    public string ConnectWebhookSigningSecret { get; init; } = string.Empty;

    /// <summary>
    /// URL Stripe redirects the artist to after a successful onboarding step.
    /// </summary>
    [Required]
    public string OnboardingReturnUrl { get; init; } = string.Empty;

    /// <summary>
    /// URL Stripe redirects the artist to when an onboarding link expires; the FE then
    /// requests a fresh link.
    /// </summary>
    [Required]
    public string OnboardingRefreshUrl { get; init; } = string.Empty;
}

namespace Needlr.Web.Services;

/// <summary>
/// FE-side Stripe configuration. Bound from the <c>Stripe</c> section of
/// <c>wwwroot/appsettings.json</c>; only the publishable key is needed here (the secret
/// key lives on the API). Empty in dev → BookingRequestForm shows a fallback message
/// rather than mounting a payment element.
/// </summary>
public sealed class StripeWebOptions
{
    public const string SectionName = "Stripe";

    public string PublishableKey { get; set; } = string.Empty;
}

using Needlr.Application.Messaging;

namespace Needlr.Application.Stripe.GenerateOnboardingLink;

/// <summary>
/// Returns a fresh hosted onboarding URL the artist visits to complete KYC. Optional
/// per-call overrides for return / refresh URLs (FE may want device-specific deep links);
/// when null, falls back to <c>StripeOptions</c> defaults.
/// </summary>
public sealed record GenerateOnboardingLinkCommand(
    string? ReturnUrl = null,
    string? RefreshUrl = null) : ICommand<string>;

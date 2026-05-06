namespace Needlr.Application.Abstractions;

/// <summary>
/// Processes a Stripe webhook payload. Implementation lives in Infrastructure where the
/// Stripe.net <c>EventUtility</c> can verify signatures and parse the event. The Api
/// controller hands raw body + signature header straight in.
/// </summary>
public interface IStripeWebhookProcessor
{
    /// <summary>
    /// Returns <see cref="StripeWebhookOutcome.Processed"/> on success (handler ran or the
    /// event was a duplicate redelivery), <see cref="StripeWebhookOutcome.InvalidSignature"/>
    /// on signature failures (controller maps to 400), or
    /// <see cref="StripeWebhookOutcome.Error"/> on parse / handler errors (controller maps
    /// to 500 so Stripe retries with backoff).
    /// </summary>
    Task<StripeWebhookOutcome> ProcessAsync(
        string payload, string signatureHeader, CancellationToken cancellationToken = default);
}

public enum StripeWebhookOutcome
{
    Processed,
    InvalidSignature,
    Error
}

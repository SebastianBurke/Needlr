namespace Needlr.Application.Abstractions;

/// <summary>
/// Wraps Stripe.NET so handlers can be unit-tested without HTTP. Per ADR-005, all calls go
/// to the artist's connected account via the <c>Stripe-Account</c> request header
/// (direct-charge model). Phase 11 implements this; Phase 3 only declares the contract.
/// </summary>
public interface IStripeService
{
    /// <summary>Creates an Express Connect account for an artist's email.</summary>
    Task<StripeConnectAccountCreated> CreateConnectAccountAsync(
        string artistEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a hosted onboarding URL (the AccountLink) the artist visits to complete KYC.
    /// </summary>
    Task<string> CreateAccountLinkAsync(
        string connectAccountId,
        string returnUrl,
        string refreshUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Pre-authorizes the deposit on the customer's payment method (capture_method=manual).</summary>
    Task<StripePaymentIntentCreated> CreatePaymentIntentAsync(
        decimal amountCad,
        string customerPaymentMethodId,
        string connectAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>Captures a previously pre-authorized PaymentIntent on the artist's connected account.</summary>
    Task<StripePaymentIntentCaptured> CapturePaymentIntentAsync(
        string paymentIntentId,
        string connectAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels (voids) a not-yet-captured PaymentIntent.</summary>
    Task CancelPaymentIntentAsync(
        string paymentIntentId,
        string connectAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds <paramref name="amountCad"/> from a captured charge associated with the given
    /// PaymentIntent. The amount may be partial per the artist's cancellation policy.
    /// </summary>
    Task<StripeRefundCreated> RefundAsync(
        string paymentIntentId,
        decimal amountCad,
        string connectAccountId,
        CancellationToken cancellationToken = default);
}

public sealed record StripeConnectAccountCreated(string ConnectAccountId);

public sealed record StripePaymentIntentCreated(string PaymentIntentId, string ClientSecret);

public sealed record StripePaymentIntentCaptured(string ChargeId, decimal AmountCapturedCad);

public sealed record StripeRefundCreated(string RefundId, decimal AmountCad);

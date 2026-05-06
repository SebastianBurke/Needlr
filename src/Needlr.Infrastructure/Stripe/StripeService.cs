using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;
using Stripe;

namespace Needlr.Infrastructure.Stripe;

/// <summary>
/// Stripe.net adapter used by command handlers. Per ADR-005 every per-account API call
/// includes the artist's connect account id via <c>RequestOptions.StripeAccount</c> so
/// charges land directly in the artist's connected account (direct-charge model).
/// </summary>
internal sealed class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly AccountService _accounts;
    private readonly AccountLinkService _accountLinks;
    private readonly PaymentIntentService _paymentIntents;
    private readonly RefundService _refunds;

    public StripeService(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        var client = new StripeClient(_options.SecretKey);
        _accounts = new AccountService(client);
        _accountLinks = new AccountLinkService(client);
        _paymentIntents = new PaymentIntentService(client);
        _refunds = new RefundService(client);
    }

    public async Task<StripeConnectAccountCreated> CreateConnectAccountAsync(
        string artistEmail, CancellationToken cancellationToken = default)
    {
        // Express account; we take care of the Account Link UI; KYC / payouts run on Stripe.
        var options = new AccountCreateOptions
        {
            Type = "express",
            Email = artistEmail,
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            },
            BusinessType = "individual",
        };
        var account = await _accounts.CreateAsync(options, cancellationToken: cancellationToken);
        return new StripeConnectAccountCreated(account.Id);
    }

    public async Task<string> CreateAccountLinkAsync(
        string connectAccountId, string returnUrl, string refreshUrl,
        CancellationToken cancellationToken = default)
    {
        var options = new AccountLinkCreateOptions
        {
            Account = connectAccountId,
            Type = "account_onboarding",
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? _options.OnboardingReturnUrl : returnUrl,
            RefreshUrl = string.IsNullOrWhiteSpace(refreshUrl) ? _options.OnboardingRefreshUrl : refreshUrl,
        };
        var link = await _accountLinks.CreateAsync(options, cancellationToken: cancellationToken);
        return link.Url;
    }

    public async Task<StripePaymentIntentCreated> CreatePaymentIntentAsync(
        decimal amountCad, string customerPaymentMethodId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = ToStripeMinorUnits(amountCad),
            Currency = "cad",
            CaptureMethod = "manual",
            PaymentMethod = customerPaymentMethodId,
            // Confirm now so Stripe collects the customer's authorization at request time
            // (per FEATURE_SPECS § Deposit handling).
            Confirm = true,
            // Off-session false: customer is on-session at request time. Stripe validates
            // SCA / 3DS up front rather than rejecting on later capture.
            OffSession = false,
        };
        var requestOptions = new RequestOptions { StripeAccount = connectAccountId };
        var intent = await _paymentIntents.CreateAsync(options, requestOptions, cancellationToken);
        return new StripePaymentIntentCreated(intent.Id, intent.ClientSecret ?? string.Empty);
    }

    public async Task<StripePaymentIntentCaptured> CapturePaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = new RequestOptions { StripeAccount = connectAccountId };
        var intent = await _paymentIntents.CaptureAsync(paymentIntentId,
            options: null, requestOptions: requestOptions, cancellationToken: cancellationToken);
        var chargeId = intent.LatestChargeId ?? string.Empty;
        return new StripePaymentIntentCaptured(chargeId, FromStripeMinorUnits(intent.AmountReceived));
    }

    public async Task CancelPaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = new RequestOptions { StripeAccount = connectAccountId };
        await _paymentIntents.CancelAsync(paymentIntentId,
            options: null, requestOptions: requestOptions, cancellationToken: cancellationToken);
    }

    public async Task<StripeRefundCreated> RefundAsync(
        string paymentIntentId, decimal amountCad, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = ToStripeMinorUnits(amountCad),
        };
        var requestOptions = new RequestOptions { StripeAccount = connectAccountId };
        var refund = await _refunds.CreateAsync(options, requestOptions, cancellationToken);
        return new StripeRefundCreated(refund.Id, FromStripeMinorUnits(refund.Amount));
    }

    /// <summary>CAD → cents (Stripe's smallest-unit representation).</summary>
    private static long ToStripeMinorUnits(decimal cad) =>
        (long)Math.Round(cad * 100m, MidpointRounding.AwayFromZero);

    /// <summary>cents → CAD.</summary>
    private static decimal FromStripeMinorUnits(long minor) => minor / 100m;
}

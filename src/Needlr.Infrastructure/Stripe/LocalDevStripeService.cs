using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Stripe;

/// <summary>
/// Local-development stub for <see cref="IStripeService"/> that returns deterministic ids
/// without calling Stripe. Selected by <see cref="DependencyInjection.AddNeedlrInfrastructure"/>
/// only when <c>Stripe:SecretKey</c> starts with the marker prefix
/// <c>sk_test_dev_localhost_</c>, so it never activates in any environment that holds a real
/// Stripe key. Lets us drive the booking-request → accept → deposit-capture flow end-to-end
/// against a local stack without a real Stripe test account.
/// </summary>
internal sealed class LocalDevStripeService : IStripeService
{
    public const string SecretKeyPrefix = "sk_test_dev_localhost_";

    private int _accountSeq;
    private int _intentSeq;
    private int _refundSeq;

    public Task<StripeConnectAccountCreated> CreateConnectAccountAsync(
        string artistEmail, CancellationToken cancellationToken = default)
    {
        var id = $"acct_devstub_{Interlocked.Increment(ref _accountSeq):D6}";
        return Task.FromResult(new StripeConnectAccountCreated(id));
    }

    public Task<string> CreateAccountLinkAsync(
        string connectAccountId, string returnUrl, string refreshUrl,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://stripe.test/onboard/{connectAccountId}");

    public Task<StripePaymentIntentCreated> CreatePaymentIntentAsync(
        decimal amountCad, string customerPaymentMethodId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var id = $"pi_devstub_{Interlocked.Increment(ref _intentSeq):D6}";
        return Task.FromResult(new StripePaymentIntentCreated(id, $"{id}_secret"));
    }

    public Task<StripePaymentIntentCaptured> CapturePaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new StripePaymentIntentCaptured($"ch_devstub_{paymentIntentId}", 100m));

    public Task CancelPaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<StripeRefundCreated> RefundAsync(
        string paymentIntentId, decimal amountCad, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        var id = $"re_devstub_{Interlocked.Increment(ref _refundSeq):D6}";
        return Task.FromResult(new StripeRefundCreated(id, amountCad));
    }
}

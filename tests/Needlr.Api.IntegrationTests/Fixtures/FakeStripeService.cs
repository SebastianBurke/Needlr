using System.Collections.Concurrent;
using Needlr.Application.Abstractions;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Records Stripe-side intent rather than calling the network. Returns deterministic ids
/// keyed by call sequence so tests can assert "the customer's pre-auth was created" without
/// depending on a real Stripe test account.
/// </summary>
public sealed class FakeStripeService : IStripeService
{
    private int _accountSeq;
    private int _intentSeq;
    private int _refundSeq;

    public ConcurrentBag<string> CapturedIntents { get; } = new();
    public ConcurrentBag<string> CancelledIntents { get; } = new();
    public ConcurrentBag<(string Intent, decimal Amount)> Refunds { get; } = new();

    public Task<StripeConnectAccountCreated> CreateConnectAccountAsync(
        string artistEmail, CancellationToken cancellationToken = default)
    {
        var id = $"acct_test_{Interlocked.Increment(ref _accountSeq):D6}";
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
        var id = $"pi_test_{Interlocked.Increment(ref _intentSeq):D6}";
        return Task.FromResult(new StripePaymentIntentCreated(id, $"{id}_secret"));
    }

    public Task<StripePaymentIntentCaptured> CapturePaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        CapturedIntents.Add(paymentIntentId);
        return Task.FromResult(new StripePaymentIntentCaptured($"ch_test_{paymentIntentId}", 100m));
    }

    public Task CancelPaymentIntentAsync(
        string paymentIntentId, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        CancelledIntents.Add(paymentIntentId);
        return Task.CompletedTask;
    }

    public Task<StripeRefundCreated> RefundAsync(
        string paymentIntentId, decimal amountCad, string connectAccountId,
        CancellationToken cancellationToken = default)
    {
        Refunds.Add((paymentIntentId, amountCad));
        var id = $"re_test_{Interlocked.Increment(ref _refundSeq):D6}";
        return Task.FromResult(new StripeRefundCreated(id, amountCad));
    }
}

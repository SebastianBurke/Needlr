using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Stripe;

/// <summary>
/// Local-development stub for <see cref="IStripeService"/> that returns deterministic ids
/// without calling Stripe. Selected by <see cref="DependencyInjection.AddNeedlrInfrastructure"/>
/// only when <c>Stripe:SecretKey</c> starts with the marker prefix
/// <c>sk_test_dev_localhost_</c>, so it never activates in any environment that holds a real
/// Stripe key. Lets us drive the booking-request → accept → deposit-capture flow end-to-end
/// against a local stack without a real Stripe test account.
///
/// <para>
/// Real Stripe advances a booking <c>Accepted → DepositCaptured → Confirmed</c> via the
/// <c>payment_intent.succeeded</c> webhook. Without a webhook in dev, accepted bookings would
/// never leave the <c>Accepted</c> state and the artist's "Upcoming" inbox section would stay
/// empty. To keep the dev flow honest, <see cref="CapturePaymentIntentAsync"/> applies the
/// same side-effects synchronously: stamp <c>DepositCapturedAt</c>, advance status, and open
/// the booking-scoped message thread (mirroring StripeWebhookProcessor.HandlePaymentIntentSucceededAsync).
/// </para>
/// </summary>
internal sealed class LocalDevStripeService(IServiceScopeFactory scopeFactory, IClock clock) : IStripeService
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
        CancellationToken cancellationToken = default)
    {
        // Real Stripe sends the payment_intent.succeeded webhook a beat after capture
        // returns, by which time the caller's transaction has already committed the
        // Accepted status. Mirror that ordering with a fire-and-forget continuation —
        // running inline would let the calling handler's SaveChangesAsync overwrite
        // Confirmed back to Accepted on its in-memory entity.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100, CancellationToken.None);
                await ApplyWebhookSideEffectsAsync(paymentIntentId, CancellationToken.None);
            }
            catch
            {
                // Dev-only stub; swallow so a transient race in tests doesn't crash the app.
            }
        }, CancellationToken.None);

        return Task.FromResult(new StripePaymentIntentCaptured($"ch_devstub_{paymentIntentId}", 100m));
    }

    private async Task ApplyWebhookSideEffectsAsync(string paymentIntentId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();

        var booking = await db.Bookings.FirstOrDefaultAsync(
            b => b.StripePaymentIntentId == paymentIntentId, cancellationToken);
        if (booking is null) return;

        var firstCapture = booking.DepositCapturedAt is null;
        if (firstCapture)
            booking.DepositCapturedAt = clock.UtcNow;

        if (booking.Status is BookingStatus.Accepted or BookingStatus.DepositCaptured)
            booking.Status = BookingStatus.Confirmed;

        if (firstCapture)
        {
            var threadExists = await db.MessageThreads
                .AnyAsync(t => t.BookingId == booking.Id, cancellationToken);
            if (!threadExists)
            {
                db.MessageThreads.Add(new MessageThread(
                    id: Guid.NewGuid(),
                    bookingId: booking.Id,
                    openedAt: clock.UtcNow));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

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

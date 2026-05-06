using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Stripe;
using Needlr.Infrastructure.Persistence;
using StripeNet = Stripe;

namespace Needlr.Infrastructure.Stripe;

/// <summary>
/// Verifies Stripe Connect webhook signatures and dispatches per event type. Idempotent:
/// every event id is recorded in <c>StripeProcessedEvents</c>; redeliveries become no-ops.
/// Implements only the events ADR-005 + FEATURE_SPECS.md call out for v1.
/// </summary>
internal sealed class StripeWebhookProcessor(
    NeedlrDbContext db,
    IClock clock,
    IOptions<StripeOptions> options,
    ILogger<StripeWebhookProcessor> logger) : IStripeWebhookProcessor
{
    private readonly StripeOptions _options = options.Value;

    public async Task<StripeWebhookOutcome> ProcessAsync(
        string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        StripeNet.Event ev;
        try
        {
            ev = StripeNet.EventUtility.ConstructEvent(
                payload, signatureHeader, _options.ConnectWebhookSigningSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeNet.StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return StripeWebhookOutcome.InvalidSignature;
        }

        // Idempotency: if we've seen this event id before, no-op. Race on the unique key
        // is handled by catching DbUpdateException after the dispatch on the off-chance two
        // workers process the same redelivered event simultaneously.
        var alreadyProcessed = await db.StripeProcessedEvents
            .AnyAsync(e => e.EventId == ev.Id, cancellationToken);
        if (alreadyProcessed)
            return StripeWebhookOutcome.Processed;

        try
        {
            switch (ev.Type)
            {
                case "account.updated":
                    await HandleAccountUpdatedAsync(ev, cancellationToken);
                    break;
                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync(ev, cancellationToken);
                    break;
                case "payment_intent.canceled":
                    // No state change required — local handlers (decline/expire) already
                    // moved the booking. Recording the event keeps the ledger consistent.
                    break;
                case "charge.refunded":
                    await HandleChargeRefundedAsync(ev, cancellationToken);
                    break;
                case "charge.dispute.created":
                    await HandleDisputeCreatedAsync(ev, cancellationToken);
                    break;
                default:
                    // Unknown event types are intentionally tolerated: subscribing too
                    // broadly is a config concern, not a bug. Record + move on.
                    logger.LogInformation("Stripe webhook {EventType} received with no handler.", ev.Type);
                    break;
            }

            db.StripeProcessedEvents.Add(new StripeProcessedEvent(ev.Id, ev.Type, clock.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
            return StripeWebhookOutcome.Processed;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Another concurrent worker beat us to the idempotency row.
            logger.LogInformation(ex, "Stripe event {EventId} processed concurrently; treating as Processed.", ev.Id);
            return StripeWebhookOutcome.Processed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe webhook handler failed for event {EventId} ({EventType}).", ev.Id, ev.Type);
            return StripeWebhookOutcome.Error;
        }
    }

    private async Task HandleAccountUpdatedAsync(StripeNet.Event ev, CancellationToken cancellationToken)
    {
        if (ev.Data?.Object is not StripeNet.Account account)
            return;

        var artist = await db.Artists.FirstOrDefaultAsync(
            a => a.StripeConnectAccountId == account.Id, cancellationToken);
        if (artist is null) return;

        // Stripe's signal that the account is fully usable (KYC complete + charges enabled).
        var newStatus = account switch
        {
            { ChargesEnabled: true, DetailsSubmitted: true } => ArtistPaymentStatus.Active,
            { ChargesEnabled: false, DetailsSubmitted: true } => ArtistPaymentStatus.Restricted,
            _ => ArtistPaymentStatus.OnboardingInProgress
        };
        if (artist.PaymentStatus != newStatus)
            artist.PaymentStatus = newStatus;
    }

    private async Task HandlePaymentIntentSucceededAsync(StripeNet.Event ev, CancellationToken cancellationToken)
    {
        if (ev.Data?.Object is not StripeNet.PaymentIntent pi)
            return;

        var booking = await db.Bookings.FirstOrDefaultAsync(
            b => b.StripePaymentIntentId == pi.Id, cancellationToken);
        if (booking is null) return;

        // Per FEATURE_SPECS § Artist response options the post-accept chain is
        // Accepted → DepositCaptured → Confirmed. The webhook is the moment funds actually
        // moved, so we stamp DepositCapturedAt here and advance to Confirmed only when the
        // booking is still mid-chain (Accepted/DepositCaptured); never demote later states.
        var firstCapture = booking.DepositCapturedAt is null;
        if (firstCapture)
            booking.DepositCapturedAt = clock.UtcNow;

        if (booking.Status is BookingStatus.Accepted or BookingStatus.DepositCaptured)
            booking.Status = BookingStatus.Confirmed;

        // Open the booking-scoped message thread. Per ADR-003 / FEATURE_SPECS § Gating,
        // the thread is created at DepositCaptured — the first webhook moment that confirms
        // funds actually moved. Idempotent: only on the first capture and only if no thread
        // already exists for this booking.
        if (firstCapture)
        {
            var existing = await db.MessageThreads
                .AnyAsync(t => t.BookingId == booking.Id, cancellationToken);
            if (!existing)
            {
                db.MessageThreads.Add(new Domain.Messaging.MessageThread(
                    id: Guid.NewGuid(),
                    bookingId: booking.Id,
                    openedAt: clock.UtcNow));
            }
        }
    }

    private async Task HandleChargeRefundedAsync(StripeNet.Event ev, CancellationToken cancellationToken)
    {
        if (ev.Data?.Object is not StripeNet.Charge charge)
            return;
        if (string.IsNullOrEmpty(charge.PaymentIntentId)) return;

        var booking = await db.Bookings.FirstOrDefaultAsync(
            b => b.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);
        if (booking is null) return;

        // Audit-only signal in v1 — we already recorded the refund decision in the cancel
        // command. Future phases may surface charge.refunded here for receipts / reconciliation.
        logger.LogInformation(
            "Stripe charge {ChargeId} refunded {Amount} for booking {BookingId}.",
            charge.Id, charge.AmountRefunded, booking.Id);
    }

    private async Task HandleDisputeCreatedAsync(StripeNet.Event ev, CancellationToken cancellationToken)
    {
        if (ev.Data?.Object is not StripeNet.Dispute dispute)
            return;

        var booking = await db.Bookings.FirstOrDefaultAsync(
            b => !string.IsNullOrEmpty(b.StripePaymentIntentId)
                && b.StripePaymentIntentId == dispute.PaymentIntentId, cancellationToken);

        // Phase 11 logs the dispute prominently for ops; Phase 15 (Trust & Safety) wires
        // it into the admin dashboard.
        logger.LogWarning(
            "Stripe dispute {DisputeId} created on payment intent {Intent} (booking {BookingId}).",
            dispute.Id, dispute.PaymentIntentId, booking?.Id);
        await Task.CompletedTask;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";
}

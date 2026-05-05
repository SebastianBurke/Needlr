# ADR-005: Individual Stripe Connect accounts only

**Status:** Accepted
**Date:** Project inception

## Context

A marketplace handling deposits has several options for money flow:

1. **Platform-held funds.** The platform receives the deposit, holds it, and pays out to the artist on a schedule (or after the session). This is what Airbnb, Uber, and many marketplaces do.
2. **Direct charges to a connected account, with a platform fee.** Stripe Connect's "destination charges" or "direct charges" route the customer's payment straight to the artist's Stripe account, with the platform optionally taking a cut via `application_fee_amount`.
3. **Studio-level Connect accounts with revenue splits to artists.** The studio receives the deposit and the platform handles splitting it to the affiliated artist (plus optionally an owner cut).

Option 1 makes the platform a money-services business in most jurisdictions, with significant regulatory burden (money transmitter licensing, custodial requirements, fund segregation).

Option 3 makes the platform a payroll intermediary, with W2/T4-equivalent obligations, dispute mediation between artists and studios, and complex tax-form generation.

Option 2 — direct charges to individual Connect accounts — keeps the platform out of the money. Stripe handles funds, the artist's Stripe account is the merchant of record for their work, and the platform is just an introduction service that happens to facilitate the payment.

The user has decided that all money flows directly to individual artist Stripe accounts, with no platform fee at launch and no studio-level account ever.

## Decision

Needlr uses Stripe Connect with the following constraints:

- **Each artist has their own Stripe Express Connect account.** Onboarding via Stripe-hosted `AccountLink` flow during artist onboarding.
- **Deposits are charged directly to the artist's connected account.** No platform routing of funds. Artist is the merchant of record.
- **No studio-level Stripe accounts. Ever.** Studios are organizational/discovery entities only; they do not receive money.
- **No platform fee at launch.** `application_fee_amount = 0`. We may add a flat artist subscription model later (off the per-transaction path), but we will not add a percentage cut to deposits.
- **Needlr never holds, splits, routes, or custodies funds.** The platform's role in payments is: presenting the Stripe Elements UI, calling Stripe's API to pre-auth/capture/cancel/refund, listening to Stripe webhooks for status updates, and showing the artist their booking history.

## Implications

**Onboarding friction.** Artists cannot accept paid bookings until they complete Stripe Connect onboarding (KYC, tax info, bank details). This is a real onboarding step — typically 5-15 minutes — that adds friction. We mitigate by:

- Marking the onboarding step prominently in the artist dashboard with a clear "complete this to start accepting bookings" call-to-action
- Showing artists in `OnboardingInProgress` state in admin tools so we can chase them if they drop off
- Surfacing the artist's `ArtistPaymentStatus` (`NotOnboarded`, `OnboardingInProgress`, `Active`, `Restricted`) on their profile

**Webhook handling is critical from day one.** Stripe webhooks tell us when:
- An artist's Connect account becomes `Active` (or gets `Restricted` for compliance reasons)
- A `PaymentIntent` succeeds, fails, or is canceled
- A charge is refunded or disputed

A webhook endpoint at `/api/webhooks/stripe` with signature verification is part of the v1 critical path. Loss of webhook events means inconsistent state.

**Real-world studio cuts are off-platform.** If a brick-and-mortar shop takes a 30% cut from each artist's earnings, that cut is handled between the shop and the artist outside of Needlr. The artist receives the full deposit into their account; whatever they owe the shop is between them. This is exactly how it works today (Square, Squarespace, individual artist-direct payment apps) and is the only way to keep Needlr out of payroll-intermediary territory.

**Tax handling is the artist's responsibility.** Stripe will issue 1099-K (US) / equivalent (Canada) tax forms based on processed volume. Needlr does not issue tax forms, does not aggregate earnings across artists for tax purposes, and is not a tax advisor.

**Disputes flow through Stripe.** A customer who chargebacks goes through Stripe's dispute process against the artist's Connect account. Needlr can advise (we have the booking record, the message thread, the description, the photos) but the merchant on the hook is the artist.

## Consequences

**Positive:**
- Needlr is not a money services business. No money transmitter licensing, no custodial requirements, no fund segregation.
- Tax forms are Stripe's problem, not ours.
- Disputes are between the customer and the artist (with Needlr as advisor), not Needlr's liability.
- Operationally simple — one Connect account per artist, no splitting logic, no payout scheduling.
- Aligned with the "we're a discovery and booking tool, not a financial intermediary" product positioning.

**Negative:**
- Onboarding friction. Some artists will drop off at the Stripe Connect step. Mitigated by clear UX and admin chase.
- We can't easily collect revenue per-transaction (would require introducing a platform fee, which we've explicitly chosen not to do at launch).
- Real-world studio-cut workflows must be handled off-platform. Some artists may find this annoying — they want their shop's cut deducted automatically. Not our problem to solve at v1.
- We don't have visibility into artist earnings except via Stripe's reporting — not directly queryable from our DB. Acceptable; we don't need that data.

## Future considerations

If we later need platform revenue:
- **Preferred:** Flat artist or studio subscription (e.g., $25/month per active artist), billed via a separate Stripe subscription on a Needlr-owned (non-Connect) account. Doesn't touch the deposit flow.
- **Avoid if possible:** Per-transaction `application_fee_amount`. Artists are extremely fee-sensitive (they already lose 30%+ to physical-shop cuts and supplies). Per-transaction fees are the dynamic that pushes artists to route customers off-platform after the first booking — exactly the dynamic we're trying to escape from Instagram.

If multiple artists later need to be paid from a single transaction (e.g., collaborative pieces): use Stripe's `transfer_data` and create separate Connect transfers post-capture. Out of scope for v1.

## Defending against future erosion

- "We need revenue, just take 5%" — see "Avoid if possible" above. Subscription before percentage.
- "Studios want a unified account for their shop" — see ADR-004. Studios are not financial entities on Needlr.
- "Customers want to pay through Needlr's brand" — branding is a UI concern; the merchant of record on the receipt is the artist, and that's correct.

## Related

- ADR-004: Artist-managed studios, no StudioOwner role
- `docs/FEATURE_SPECS.md` § Bookings § Deposit handling
- `docs/ARCHITECTURE.md` § Stripe Connect

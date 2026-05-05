# ADR-003: Message privacy and booking-scoped messaging

**Status:** Accepted
**Date:** Project inception

## Context

Tattoo artists currently receive most customer inquiries via Instagram DMs. This is bad for several reasons: incomplete briefs ("hey i love ur work, i was thinking maybe a snake on my forearm, when r u free"), inability to handle deposits, no structured booking record, and indefinite parasocial channels with strangers.

Needlr could either:
1. Have no in-product messaging at all — customers and artists exchange contact info post-booking and communicate elsewhere.
2. Have full open messaging — anyone can DM anyone.
3. Have scoped messaging tied to bookings — communication only opens after a transactional commitment.

Option 1 puts us back in the position of being a glorified contact form, with no visibility into the customer-artist relationship and no ability to support disputes or moderation.

Option 2 reintroduces every problem with Instagram DMs (parasocial dynamics, fragmented briefs, harassment surface) and adds a moderation burden we don't want to take on.

Option 3 — the Fiverr model — preserves the booking-as-transaction nature of the product, gives both parties a working channel for the things they actually need to coordinate, and keeps the moderation surface bounded.

## Decision

Messaging on Needlr is gated by paid bookings. Specifically:

- A customer cannot send a message to an artist before a booking is created.
- The booking request form is the structured first contact. It enforces a complete brief (description, references, body placement, size, requested date).
- During the pre-acceptance phase, the artist can respond only with structured options: Accept, Decline (with reason from a short list), or Request More Info (structured prompt for missing data). No free-text messaging in this phase.
- The message thread opens only when the booking reaches `DepositCaptured` (artist accepted, deposit captured via Stripe).
- The message thread is scoped to that specific booking. There is no persistent "DM with this artist" relationship.
- Each new booking — including touch-ups and repeat sessions with the same artist — opens its own thread.
- 90 days after the booking reaches a terminal state (Completed, Cancelled, etc.), the thread becomes read-only, then locked.

## Privacy

Messages are private between the parties to the thread and Needlr admins. Specifically:

- Other users (including other artists at the same studio) cannot see thread contents.
- Needlr admins can read thread contents only in response to a report (filed by either party) or a legal request.
- Messages are never used for any other purpose — no analytics, no machine learning training, no feature personalization.

## Retention

Message and attachment retention are intentionally asymmetric:

- **Message text bodies are retained indefinitely** in the database, with admin-only read access after the thread locks. This is required to support the trust-and-safety case — admins must be able to review historical patterns when adjudicating later reports or appeals, and a suspended user's appeal cannot be evaluated fairly without the underlying message record.
- **Attachment blobs** (`BookingAttachment.Url` and the underlying file in object storage) are purged 1 year after the booking reaches a terminal state. The `BookingAttachment` DB record is retained with `Url` cleared so booking history remains coherent.
- **Reasoning:** attachment blobs hold most of the storage cost and most of the privacy-sensitive data (reference images, design drafts, photos of body). Message text is small and bears the moderation case. Purging blobs aggressively while keeping text indefinitely is the policy that best balances privacy and accountability.

## Pre-booking content stripping

The booking request description field has phone numbers, email addresses, and social handles regex-stripped at submit time, with a friendly inline message: "Contact info will be shared automatically once your booking is confirmed."

This prevents the pre-booking funnel from leaking off-platform. Once the booking is confirmed and the message thread opens, no stripping is applied — adults can exchange phone numbers for day-of logistics if they want. The point is to keep the funnel intact, not to lock people in forever.

### `DeclineNote` is not messaging

The artist's optional free-text `DeclineNote` on a Decline action looks superficially like free-text messaging, but it is a structured one-shot field: bounded (single submission attached to the booking record), one-directional (artist → customer, no reply channel), and exists only so the artist can offer a brief courtesy explanation beyond the `DeclineReason` enum. The same regex strip applied to the booking request description **also applies to `DeclineNote`** — the funnel-protection rationale is identical.

## Channel choice

Async only in v1. Email and Web Push notifications on new messages. No SignalR, no typing indicators, no presence, no read receipts beyond a `ReadAt` timestamp surfaced as "Read" in the UI.

Tattoo communication is naturally async (artists check messages between sessions, not while tattooing). Real-time infrastructure is not justified at v1 scale.

## Consequences

**Positive:**
- Artists are protected from infinite unpaid pre-booking consultations. Every conversation has a transactional anchor.
- Customers get a complete brief on first contact, which artists prefer.
- The platform stays out of the relationship after the work is done. No "ongoing conversation" surface that becomes a parasocial channel.
- Moderation surface is bounded — only paying customers can message, and only within the context of a specific booking.
- Trust & safety has clear authority to act on reported messages because all messages exist in a paid commercial relationship.

**Negative:**
- Customers cannot ask "is this artist a good fit before I commit?" in messages. They have to make the judgment from the portfolio, behavioral signals, and bio. The booking request form's "Description" field is the place to surface concerns and questions, and the artist can decline if the fit isn't right (no money is captured pre-acceptance).
- Artists cannot proactively reach out to past customers (e.g., "hey, your sleeve is due for a touch-up"). They have to rely on the customer initiating a Touch-up booking. We accept this — proactive outreach to past customers is exactly the kind of relationship-mediation we don't want to be in the middle of.
- Some customer inquiries that would have become bookings on Instagram won't on Needlr because the customer wasn't ready to commit to a deposit pre-auth. This is a real cost. We bet that the higher quality of inbound (complete briefs, real intent) outweighs the lower volume.

## Defending against future erosion

Likely future arguments for opening up messaging:

- "Just for verified-customer-of-this-artist-once" — this becomes "long-term DM with my regular artist" which is exactly the parasocial dynamic we're avoiding. Touch-up bookings are the legitimate channel for re-engagement.
- "Just for studio admins to coordinate with affiliated artists" — internal studio coordination is out of scope for Needlr. Studios can use Slack/text/whatever for internal ops.
- "Just for pre-booking questions" — this re-creates Instagram DMs. The booking request form is the channel for pre-booking communication.

## Related

- ADR-001: No social features
- `docs/FEATURE_SPECS.md` § Messaging
- `docs/DOMAIN_MODEL.md` § MessageThread, Message

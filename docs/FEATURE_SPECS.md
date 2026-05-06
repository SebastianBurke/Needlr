# Needlr — Feature Specifications

This document captures every feature-level decision made during planning. Anything not in this document is out of scope for v1. Anything contradicting this document violates a planning decision and should be raised before being built.

## Verification

### Tiered model
Three verification tiers visible to admins; two visible to customers (verified or not).
- `Unverified` — no documents submitted, or some submitted but none verified
- `DocumentsSubmitted` — at least one document uploaded, awaiting admin review
- `Verified` — all required documents reviewed and approved by admin

### Required credentials (Montréal)
**Studio level:**
- RSSS health inspection certificate (annual renewal)
- Municipal registration (one-time, but document must be on file)

**Artist level:**
- Bloodborne pathogen certification (annual renewal)
- Formation hygiène et salubrité (recommended, not strictly required by law but used as a quality signal)

Note: Quebec does not license individual tattoo artists, so the Montréal launch focuses on training/hygiene credentials. Other jurisdictions configure `RequiresArtistLicense = true` on their `Jurisdiction` row.

### Discoverability rules
- An artist appears in discovery results only if their **computed verification status** is at least `DocumentsSubmitted` — meaning their primary studio's status is at least `DocumentsSubmitted` AND they have at least one credential of each required type uploaded with status above `Unverified`/`Rejected`.
- The **Verified-only filter** is on by default. When on, results are restricted to artists whose computed status is `Verified`. When off, results include both `Verified` and `DocumentsSubmitted`.
- `Unverified` and `Rejected` statuses are **never** shown in discovery — only in admin tools.
- A previously-verified artist whose primary studio loses verification (expired inspection, etc.) drops to `DocumentsSubmitted` (visible only when the filter is off) during the 7-day grace period, then to `Unverified` (invisible) once the grace period elapses.

### Expiry handling
- 30 days before any credential expiry: notification email to the credential owner (artist or studio admin)
- 7 days before expiry: second notification
- On expiry day: credential becomes `Expired`; the affected artist/studio loses Verified status
- 7-day grace period after expiry where the artist/studio shows as `DocumentsSubmitted` (not invisible) before being downgraded to `Unverified`. This buys time for renewals in flight.

### Admin verification workflow
- New document upload triggers a record in the admin verification queue
- Admin sees the document, the credential metadata, and the entity (artist or studio)
- Admin actions: Approve, Reject (with reason from a short list + free text), Request resubmission
- Approval sets `VerificationStatus = Verified`, `VerifiedAt`, `VerifiedByAdminId`
- Rejection sets `VerificationStatus = Rejected` and notifies the credential owner with the reason

### Adding markets
The `Jurisdiction` entity exists from day one with Montréal as the only seeded row. Adding markets means seeding new jurisdictions and configuring their `RequiresXxx` flags — no schema changes required. Implementation work that is **not yet decoupled** from the Montréal launch and must be revisited before a non-CAD market ships:
- **Currency**: bookings carry `*Cad` field names (`DepositAmountCad`, `EstimatedTotalCad`) and `IStripeService` hardcodes `"cad"`. Parameterize per-jurisdiction.
- **Default map center**: discovery falls back to Montréal coordinates when geolocation is unavailable (see § Discovery > Map view). Make per-jurisdiction.
- **Locale**: no fr/en infrastructure today.
- **Admin tooling**: jurisdictions are seeded once via `DataSeeder`; managing them in production needs an admin surface.

## Discovery

### Map view (primary)
- Default-centered on user's geolocation if available, otherwise on the launch-market center (Montréal: lat 45.5019, lng -73.5674; per-jurisdiction default deferred until a second market is seeded)
- MapLibre GL JS rendering, MapTiler tiles
- Studio pins are the primary entity on the map (not individual artists). Click a pin to see the studio's roster.
- Bounding-box queries: when the user pans or zooms, refire the spatial query with the new viewport bounds (debounced 300ms)
- Pin clustering at low zoom levels
- Solo/Private studios appear as pins exactly like Shop studios — no visual differentiation in v1 (they're all "places where you can get a tattoo")

### Filters
Filters available, applied as AND:
- **Style** — multi-select chip against `TattooStyle` (canonical only). Returns studios with at least one affiliated artist tagged with any selected style.
- **Verified** — boolean toggle, default on. When on: only Verified studios+artists. When off: Verified + DocumentsSubmitted (never Unverified).
- **Accepting bookings** — boolean toggle, default on. Returns studios with at least one affiliated artist who has `AcceptingNewBookings = true`.
- **Walk-ins welcome** — boolean toggle, default off. Returns studios with `Studio.AcceptsWalkIns = true`. Walk-ins is a venue-level policy on the studio itself, not a per-artist commitment.
- **Availability** — date range picker (default: next 30 days). Returns studios with at least one affiliated artist who has at least one `IsBookable = true` day in the date range from `ArtistAvailabilityProjection`.

### Sort
**Single fixed ordering: earliest bookable date first, distance from map center as tiebreaker.** Sort is not user-configurable.

Rationale: distance ordering is largely redundant when the bounding-box query already restricts results to the visible viewport. Verified-first ordering has no customer-facing meaning beyond the binary verified/unverified filter. Availability is the signal customers actually evaluate when picking between candidates, so it drives the order. Studios with no projection rows in the requested window fall through to the distance tiebreaker, so the ordering remains stable even before Hangfire has projected forward.

**Sorting by price is explicitly removed.** Rationale: price correlates with experience/popularity and ranking by price would inversely rank quality.

### List view (secondary)
- Toggleable from the map view; on mobile, a draggable bottom sheet
- Synced to map's visible bounds — same query, different presentation
- Each entry: studio name, primary photo, distance, list of affiliated artists with thumbnails, top 1-2 styles practiced, verification badge

### Browse without account
- Customer can browse the map, filter, view studio profiles, view artist profiles, and view portfolios without an account
- Account required only at the moment of submitting a booking request
- The account-creation flow at booking-request time should not lose the in-progress booking draft

## Onboarding

### Customer onboarding
- Zero friction: browse without account, sign up only when booking
- Sign up: email + password OR Google OAuth (deferred — v1 launches with email/password only; OAuth is v1.1 unless trivial)
- Optional after signup: set home location, preferred styles, search radius
- No mandatory profile completion gate

### Artist onboarding
Heavy, multi-step. The artist cannot accept paid bookings until all steps are complete.

1. **Account creation** — email + password, role = Artist
2. **Profile basics** — display name, bio, years experience, hourly rate, shop minimum
3. **Style selection** — pick from canonical TattooStyles (multi-select, max 6 to keep profiles focused)
4. **Studio choice** — three options:
   - Join existing: search by name/location. For `Open` studios, the artist submits a join request and waits for studio-admin approval. For `InviteOnly` studios, the artist must already have a pending invitation. `Closed` studios are not joinable.
   - Create new: become founder + admin of a new studio (next step prompts for studio info)
   - Solo/Private: create a `Solo` (single-artist working location) or `Private` (invite-only collective) studio with the artist as founder
5. **Studio info** (only if creating new) — name, location, address, hours, join policy, type
6. **Credential uploads** — bloodborne pathogen cert minimum; formation hygiène recommended; studio inspection if founder of a new studio
7. **Stripe Connect onboarding** — redirected to Stripe's hosted Connect flow, returns with `StripeConnectAccountId` and `PaymentStatus = OnboardingInProgress` until Stripe webhook confirms `Active`
8. **Portfolio seed** — minimum 5 portfolio pieces required to appear in discovery (each with photo, style tag, body placement)
9. **Availability setup** — recurring weekly pattern (which days work which hours), any initial overrides
10. **Cancellation policy choice** — Strict / Standard / Flexible (default Standard)
11. **Lead time per booking type** — defaults: Consultation 3 days, TattooSession 7 days, Touchup 7 days; artist can override

The artist appears in discovery only after: account verified, profile complete, primary studio Verified, at least one Verified credential, Stripe `Active`, and at least 5 portfolio pieces.

### No StudioOwner onboarding
There is no separate onboarding flow for non-tattooing studio owners. A studio is created by an artist and managed by artist-admins. See ADR-004.

## Bookings

### Customer-initiated request flow
1. Customer is on an artist's profile (already past discovery filters)
2. Click "Request Booking"
3. Structured form:
   - Booking type (only TattooSession enabled in v1)
   - Description (required, max 5000 chars; **regex strip** of phone numbers, emails, and social handles with friendly inline message)
   - Body placement (required, from enum)
   - Approximate size in cm (optional)
   - Estimated session length (optional, with default suggestions based on size + placement)
   - Requested date (required; cannot be earlier than artist's `MinimumLeadTimeDays` for this booking type)
   - Reference image uploads (optional, up to 8, jpg/png/webp, max 10MB each)
4. Customer reviews the artist's cancellation policy (clearly displayed)
5. Customer enters payment method (Stripe Elements)
6. **Pre-authorize the deposit** via Stripe (amount = artist's deposit setting, default $100 CAD)
7. Booking submitted; status = `Requested`
8. Artist notified by email and in-app

### Artist response options
Artist sees the request in their dashboard. Options:
- **Accept** — confirms a specific date and optionally a start time. Triggers deposit capture. Status → `Accepted` → `DepositCaptured` → `Confirmed` (these may be sub-second apart in practice). MessageThread opens.
- **Decline** — picks reason from enum (`NotAcceptingBookings`, `OutsideMyStyle`, `ScheduleConflict`, `Other`) plus optional note. Voids pre-auth. Status → `Declined`.
- **Request More Info** — sends a structured prompt with checkboxes for what's missing (better references, clearer size, alternative dates). Status → `AwaitingCustomerInfo`. No free-text in this phase.

If no response within 7 days: pre-auth voided, status → `Expired`, customer notified.

### Deposit handling
- Pre-auth at request time on customer's card via Stripe Payment Intent (`capture_method: manual`)
- Capture on artist acceptance
- Funds go directly to artist's connected Stripe account; Needlr takes no fee at launch
- Voided automatically on decline or expiry
- Cancellation handling per the artist's `CancellationPolicySnapshot` on the booking:
  - **Strict**: deposit non-refundable on any customer cancellation; full refund only on artist cancellation
  - **Standard**: full refund if customer cancels >7 days before; non-refundable inside 7 days; full refund on artist cancellation
  - **Flexible**: full refund if customer cancels >48 hours before; non-refundable inside 48 hours; full refund on artist cancellation
- All cancellation refunds via Stripe; record kept on the booking

### Booking lifecycle post-confirmation
- `Confirmed` → MessageThread opens, both parties can message
- 24 hours before `ConfirmedSessionDate`: reminder email + push to both parties
- Session day: artist can mark `InProgress` (optional, for their own tracking)
- Post-session: artist marks `Completed`. Triggers customer feedback prompt (private). Schedules healed-photo prompt for 4 months out.
- 90 days after the booking reaches a terminal state: `MessageThread.Status` transitions to `Locked`. Existing messages remain visible to the parties; no new messages may be sent.
- 1 year after terminal state: **attachment blob storage is purged** for `BookingAttachment` records (the underlying file in object storage is deleted; the DB record's `Url` is cleared and `Booking.IsAttachmentsPurged` is set). **Message text bodies are retained indefinitely** with admin-only access; only blobs are purged. See ADR-003 § Retention for rationale.

### BookingType scaffolding
All three types (`Consultation`, `TattooSession`, `Touchup`) exist in the schema. Only `TattooSession` is enabled in the v1 UI. The other two have `IsEnabled` flags somewhere in config — easy to flip on later without code changes.

## Portfolio

### Piece-first model
A `PortfolioPiece` is the unit. Both artist-grid view and studio-collective view are projections over pieces.

### Photo handling
- **Fresh photo**: uploaded by artist after session. Required for the piece to be visible.
- **Healed photo**: uploaded by customer 4 months after session via prompted reminder (email + push). Optional but encouraged via UX (the artist's "% of pieces with healed photos" is a behavioral signal on their profile).
- Both display paired on the piece detail view, with clear "Fresh" and "Healed" labels.
- Multi-session pieces: ordered collection of `SessionPhoto` entries; each can be Fresh or Healed.

### Customer-uploaded photo policy
- Artist can hide a customer-uploaded photo only for content policy violations (NSFW, contains identifying information of a third party, off-topic). Hiding requires selecting a reason; admin can audit.
- Artist cannot hide a customer photo because it's "unflattering" or shows poor healing — that defeats the entire trust mechanism.
- Customer can re-upload a different photo if they want.

### Tagging
- **Styles**: many-to-many with canonical `TattooStyle`. At least one required.
- **Freeform tags**: up to 3 per piece, lowercase, alphanumeric+hyphens. Searchable but not in the filter dropdown. Admin tool to promote popular freeforms to canonical.
- **Body placement**: from enum, required.
- **Size, session length, year**: optional metadata.

### Portfolio prerequisites
- Artist must have at least 5 portfolio pieces (each with at minimum a Fresh photo + style + body placement) to appear in discovery.
- No upper limit, but pagination after 50 pieces on the artist grid view.

## Messaging

See ADR-003 for the privacy stance.

### Gating
- Customer cannot message artist before booking.
- Booking request form is the structured first contact.
- MessageThread opens on `DepositCaptured`.

### Channel
- Async only (no SignalR in v1).
- Email notification on new message in your active thread.
- Push notification (Web Push API, PWA-installed users only) on new message.
- In-app indicator (unread count badge).

### Attachments
- Same `BookingAttachment` entity as booking-request attachments — a single dual-use type with nullable `BookingId` and `MessageId` foreign keys. Exactly one of the two FKs is set per row.
- Artists upload design drafts here.
- **Blob storage purged 1 year post-booking-terminal-state.** The DB record is retained (with `Url` cleared) for audit; only the underlying file in object storage is deleted.

### Pre-acceptance content stripping
- Booking request description: regex strip phone numbers, emails, social handles. Inline message: "Contact info will be shared automatically once your booking is confirmed."
- Post-acceptance message thread: no stripping. Adults can exchange phone numbers for day-of logistics.

### Moderation
- Either party can report a message. Report routed to admin queue.
- Admin can soft-hide messages, warn users, suspend users.
- No automated content moderation in v1 (volume too low).
- **Message text bodies are retained indefinitely** in the DB (admin-only access) so admins can review historical patterns when adjudicating later reports or appeals. Only attachment blobs are purged at the 1-year-post-terminal mark. See ADR-003 § Retention.

## Trust & Safety

### Private feedback
After a booking reaches `Completed`, the customer is prompted (email + in-app) to submit a `BookingFeedback`. Optional, not gated.
- Communication 1-5
- Cleanliness 1-5
- Respected design brief 1-5
- Would book again y/n
- Optional free text

Stored privately. Never shown to artist or other customers. Drives an admin trust & safety dashboard:
- Artists with multiple low scores (<3 average across last 10 feedbacks) flagged for review
- Artists with multiple "would not book again" responses flagged
- Free text containing safety keywords (TBD list — harassment, hygiene concerns, etc.) flagged

### Behavioral signals (shown publicly on artist profiles)
Computed from platform data, not from customer opinions:
- **Response time** — median time from request to first action (Accept/Decline/RequestInfo) over last 30 days. Displayed as "Usually responds in X hours."
- **Completion rate** — % of confirmed bookings that reached `Completed` over last 90 days. Only shown if ≥10 bookings in window.
- **Healed photo rate** — % of completed bookings (≥4 months old) where the customer uploaded a healed photo. Only shown if ≥10 eligible bookings.
- **Repeat client rate** — % of customers who book a second session within 12 months. Only shown if ≥20 unique customers.

These show as small badges or a "track record" section on the artist profile. They never include customer-attributed text or scores.

### Admin actions
- Verify/reject credentials
- Hide reported messages
- Warn users
- Suspend artists (artist becomes invisible in discovery, can't accept new bookings; existing bookings honored)
- Suspend customers (can't make new bookings; existing bookings honored)
- Permanent ban (last resort)

## Availability

### Per-day model
Day-based availability with capacity. Each day for an artist resolves to one of:
- Available (with optional max session-hours)
- Limited (with max session-hours, signaling "I have some time but not much")
- Closed

### Inputs
- **`AvailabilityPattern`**: recurring weekly. E.g., Tue-Sat Available, Sun-Mon Closed.
- **`AvailabilityOverride`**: specific dates. E.g., closed for vacation Aug 1-15.
- **`BookingWindow`** (optional): batched-booking model. If any windows defined, requests only accepted during open windows for sessions within the window's target range.
- **Existing bookings**: consume capacity from the day's `MaxSessionHours`.

### Projection
`ArtistAvailabilityProjection` flattens all of the above to a per-artist-per-day boolean (`IsBookable`) + `RemainingSessionHours`.
- Rebuilt nightly by Hangfire for a rolling 90-day window.
- Recomputed on-demand when:
  - Artist updates their pattern
  - Artist adds/removes an override
  - Artist creates/closes a booking window
  - A booking is accepted, cancelled, or completed
- The discovery availability filter queries this projection only.

### Lead time
`ArtistLeadTime` per `BookingType`. Booking requests are rejected at form-submit time if requested date < today + lead time.

### iCal export
One-way export of confirmed bookings as an iCal feed. Each artist gets a tokenized URL they can subscribe to in Google Calendar / Apple Calendar / etc. No two-way sync in v1.

## Studios

### Lifecycle
- An artist creates a studio during onboarding. They become its `Founder` + `Admin`.
- Other artists can join based on the studio's `JoinPolicy`:
  - `Open`: a verified artist may submit a join request; a studio admin then approves or rejects. Artists can initiate, but the admin retains final say over the roster (no auto-approval — studios should never lose control of who appears on their pin).
  - `InviteOnly`: only studio admins can initiate, via invitations to specific artists; the invited artist accepts or declines. Unsolicited join requests are not accepted.
  - `Closed`: no new members. Existing members only; no requests, no invites.
- Admins can promote/demote members, change JoinPolicy, edit studio info, upload credentials.
- Founder cannot be removed except by ceding founder status to another admin first.

### Guest spots
- A guest spot is an `ArtistStudioAffiliation` with `AffiliationType = GuestSpot` and a non-null `EndDate`.
- Initiated by either the visiting artist requesting or the host studio admin inviting.
- Approved by the other party.
- During `[StartDate, EndDate]`, the visiting artist appears on the host studio's roster and pin. Their portfolio remains their own.
- Outside the window, the affiliation is `Ended` and they don't appear at the host.

### Studio types
- **Shop**: traditional brick-and-mortar with multiple artists. Default `JoinPolicy = InviteOnly`; admin may switch to `Open` to let other artists request to join the roster.
- **Solo**: a single-artist working location (artist's own private studio with just them). Default `JoinPolicy = Closed` — a Solo studio is by definition single-artist; to add members, the type must be changed to `Shop` first.
- **Private**: studio that is invite-only and not publicly walk-in-able. Default `JoinPolicy = InviteOnly`.

`JoinPolicy` governs the artist roster (who can join), not customer-facing walk-up policy. **Customer walk-ins are gated by `Studio.AcceptsWalkIns`** — independent of `JoinPolicy` and `StudioType`. A `Shop` with `AcceptsWalkIns = true` advertises that customers can drop in without an appointment; the discovery walk-ins filter surfaces it. Default is `false` on creation.

These are largely informational at launch; they may drive UI variations later (e.g., Solo studios show artist name as the headline instead of studio name).

## Notifications

### Channels
- **Email** (always available, default on)
- **Web Push** (PWA installed users on Android and iOS 16.4+)

### Notification types (per-channel toggles)
- New booking request (artists only)
- Booking accepted (customers)
- Booking declined (customers)
- Booking expired (customers)
- New message in your active thread
- Booking reminder 24 hours prior (both parties)
- Healed photo upload prompt at 4 months (customers)
- Credential expiring soon — 30 days (artists/studio admins)
- Credential expiring soon — 7 days (artists/studio admins)
- Credential expired (artists/studio admins)
- Verification approved (artists/studio admins)
- Verification rejected (artists/studio admins)
- Studio join request (admins)
- Studio join approved/rejected (artist who requested)
- Guest spot invitation (visiting artist)
- Guest spot accepted/rejected (host admin)

### PWA install prompt
Customer app: prompted after the first booking is `Confirmed`.
Artist app: prompted after Stripe Connect onboarding completes.

## v2+ explicitly out of scope

Listed here so we don't accidentally build them:
- Aggregate trust badges
- SignalR real-time messaging
- Adding markets beyond Montréal — schema is ready; missing pieces are jurisdiction admin UI, per-jurisdiction currency, and locale tooling (see § Adding markets)
- Studio chair-level capacity
- Two-way calendar sync
- Native iOS/Android apps
- Studio-level Stripe accounts or revenue splitting
- Public reviews of any kind
- Pinterest-style inspiration board
- AI-assisted style matching
- In-app design tools
- Subscription monetization (revisit at ~50 active studios with repeat bookings)

## Tattoo style canonical seed list

Seed `TattooStyle` with these as `IsCanonical = true`:
American Traditional, Neo-Traditional, Japanese (Irezumi), Blackwork, Dotwork, Geometric, Fineline, Single Needle, Microrealism, Realism, Black & Grey Realism, Color Realism, Portrait, Watercolor, Trash Polka, Illustrative, New School, Sketch, Linework, Ornamental, Tribal, Polynesian, Chicano, Lettering / Script, Religious / Spiritual, Surrealism, Botanical, Animal, Anime / Manga, Ignorant Style, Cybersigilism, Abstract.

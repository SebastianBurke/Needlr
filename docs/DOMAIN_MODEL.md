# Needlr — Domain Model

This is the authoritative domain model for v1. All entities live in `Needlr.Domain` with no external dependencies (except `NetTopologySuite.Geometries` for spatial types). Properties listed are required unless marked optional. Behavior (methods, domain events) is intentionally not enumerated here — add it as it becomes necessary, but keep it in the Domain layer.

## Identity & users

### `User`
The authentication record. **Lives in `Needlr.Infrastructure`, not in `Needlr.Domain`** — implemented as `ApplicationUser : IdentityUser<Guid>` because ASP.NET Core Identity types cannot be referenced from Domain (Domain has zero external dependencies except `NetTopologySuite.Geometries` per `docs/ARCHITECTURE.md` § Layering rules).
- `Id` (Guid) — inherited from `IdentityUser<Guid>`
- `Email`, `PasswordHash`, `EmailConfirmed` — inherited from `IdentityUser<Guid>`
- `CreatedAt` (DateTime, UTC) — added on `ApplicationUser`
- `Role` (enum: `Customer`, `Artist`, `Admin`) — added on `ApplicationUser`; the `UserRole` enum itself lives in `Needlr.Domain.Enums`
- One-to-one with either `CustomerProfile` or `Artist` depending on role

Domain entities reference users by `UserId : Guid` foreign key only; there is no `User` navigation property from any Domain entity.

### `CustomerProfile`
- `Id`
- `UserId` (FK to User, one-to-one)
- `DisplayName`
- `Location` (Point, optional — used as default for map centering)
- `PreferredSearchRadiusKm` (default 15)
- `PreferredStyles` (many-to-many to `TattooStyle`, optional)

### `Artist`
- `Id`
- `UserId` (FK to User, one-to-one)
- `DisplayName`
- `Bio` (text, max 2000 chars)
- `YearsExperience` (int)
- `HourlyRateCad` (decimal, optional — published rate)
- `ShopMinimumCad` (decimal, optional — floor price for any piece)
- `AcceptingNewBookings` (bool, default true)
- `PaymentStatus` (enum: `NotOnboarded`, `OnboardingInProgress`, `Active`, `Restricted`)
- `StripeConnectAccountId` (string, nullable)
- `Affiliations` (collection of `ArtistStudioAffiliation`)
- `Styles` (many-to-many to `TattooStyle`)
- `MinimumLeadTimeDays` (per-`BookingType` collection — see `ArtistLeadTime`)
- `CancellationPolicy` (enum: `Strict`, `Standard`, `Flexible`, default `Standard`)
- `IcalToken` (string, nullable — opaque token gating the per-artist iCal feed URL; lazily generated on first request, rotatable to invalidate subscribed calendar clients per `docs/FEATURE_SPECS.md` § iCal export)

Note: an artist's `Location` is derived from their primary studio affiliation, not stored on the artist directly. Solo artists have a Solo-type studio whose location is their working location.

### `ArtistLeadTime`
- `Id`
- `ArtistId` (FK)
- `BookingType` (enum)
- `MinimumDays` (int)

## Studios

### `Studio`
- `Id`
- `Name`
- `StudioType` (enum: `Shop`, `Solo`, `Private`)
- `Location` (Point, required)
- `Address` (string, for display)
- `JoinPolicy` (enum: `Open`, `InviteOnly`, `Closed`)
- `AcceptsWalkIns` (bool, default `false`) — venue-level walk-up policy. When true, the studio is surfaced to discovery searches that filter for walk-ins. Studio admins toggle this.
- `Description` (text, optional)
- `Hours` (collection of `StudioHours`)
- `Affiliations` (collection of `ArtistStudioAffiliation`)
- `Credentials` (collection of `StudioCredential` — e.g., RSSS health inspection)
- `CreatedByArtistId` (FK to Artist — the founder; informational only, admin rights are on the affiliation)

### `StudioHours`
- `Id`
- `StudioId` (FK)
- `DayOfWeek`
- `OpenTime`
- `CloseTime`
- `IsClosed` (bool — true means the studio is closed that day regardless of times)

### `ArtistStudioAffiliation`
The relationship between an artist and a studio. Time-boxed to support guest spots.
- `Id`
- `ArtistId` (FK)
- `StudioId` (FK)
- `Role` (enum: `Founder`, `Admin`, `Member`)
- `AffiliationType` (enum: `Permanent`, `GuestSpot`)
- `StartDate`
- `EndDate` (nullable — null means indefinite)
- `Status` (enum: `Pending`, `Active`, `Ended`, `Rejected`)
- `IsPrimary` (bool — an artist can be affiliated with multiple studios but has one primary; their default location for discovery)

A `Founder` is the artist who created the studio. A `Founder` is always also an `Admin` for permission checks. `Founder` is informational; `Admin` is the permission level.

`GuestSpot` affiliations have a non-null `EndDate` and the visiting artist appears on the host studio's roster only during `[StartDate, EndDate]`.

## Verification

### `Jurisdiction`
- `Id`
- `Name` (e.g., "Montréal, Quebec, Canada")
- `Country`
- `Region`
- `City`
- `RequiresStudioInspection` (bool)
- `RequiresArtistLicense` (bool)
- `RequiresArtistHygieneTraining` (bool)
- `RequiresBloodbornePathogenCert` (bool)

Seeded with Montréal as the only launch-market row: `RequiresStudioInspection = true`, `RequiresArtistLicense = false` (Quebec doesn't license individual artists), `RequiresArtistHygieneTraining = true`, `RequiresBloodbornePathogenCert = true`. Adding additional jurisdictions is a data-only change — no schema migration required.

### `StudioCredential`
- `Id`
- `StudioId` (FK)
- `JurisdictionId` (FK)
- `CredentialType` (enum: `HealthInspection`, `MunicipalRegistration`, `Other`)
- `DocumentUrl` (string — blob storage URL)
- `IssuedDate`
- `ExpiryDate`
- `VerificationStatus` (enum: `Unverified`, `DocumentsSubmitted`, `Verified`, `Rejected`, `Expired`)
- `VerifiedByAdminId` (FK to User, nullable)
- `VerifiedAt` (datetime, nullable)
- `RejectionReason` (string, nullable)

### `ArtistCredential`
- `Id`
- `ArtistId` (FK)
- `JurisdictionId` (FK)
- `CredentialType` (enum: `BloodbornePathogenCertification`, `FormationHygieneEtSalubrite`, `LicensePractitioner`, `Other`)
- `DocumentUrl`
- `IssuedDate`
- `ExpiryDate`
- `VerificationStatus` (same enum as above)
- `VerifiedByAdminId`
- `VerifiedAt`
- `RejectionReason`

### Computed verification status

An `Artist`'s overall verification status (used by the discovery filter) is derived:
- `Verified` only if the artist's primary studio is `Verified` AND the artist has at least one `Verified` credential of each type required by the studio's jurisdiction
- `DocumentsSubmitted` if any required credentials are uploaded but not all verified
- `Unverified` otherwise

Expiry: A credential within 30 days of expiry triggers a warning notification but stays `Verified`. After expiry it becomes `Expired` (treated as `Unverified` for discovery purposes).

## Portfolio

### `TattooStyle`
- `Id`
- `Name` (e.g., "Japanese", "Blackwork", "Fineline")
- `Slug` (URL-safe)
- `IsCanonical` (bool — true for the seeded ~30; false for promoted freeform tags)

Seed list in `docs/FEATURE_SPECS.md`.

### `BodyPlacement` (enum, not entity)
Forearm, UpperArm, FullSleeve, HalfSleeve, Hand, Finger, Shoulder, Chest, Sternum, UpperBack, LowerBack, FullBack, Ribs, Stomach, Hip, Thigh, Calf, Ankle, Foot, Neck, Throat, Head, Face, Other.

### `PortfolioPiece`
- `Id`
- `ArtistId` (FK)
- `Title` (optional)
- `Description` (optional)
- `Styles` (many-to-many to `TattooStyle` — at least one required)
- `FreeformTags` (collection of strings, max 3)
- `BodyPlacement` (enum, required)
- `ApproximateSizeCm` (int, optional)
- `EstimatedSessionLengthHours` (decimal, optional)
- `YearCompleted` (int)
- `ProgressionStatus` (enum: `SingleSession`, `MultiSessionInProgress`, `MultiSessionComplete`)
- `Sessions` (ordered collection of `SessionPhoto`)
- `LinkedBookingId` (FK to Booking, nullable — set when the piece originates from a Needlr booking)
- `CreatedAt`

Note the deliberate absence of `LikeCount`, `Comments`, `ShareCount`, `IsFeatured`, etc.

### `SessionPhoto`
- `Id`
- `PortfolioPieceId` (FK)
- `Order` (int — for multi-session display order)
- `PhotoType` (enum: `Fresh`, `Healed`)
- `ImageUrl` (string)
- `UploadedByUserId` (FK to User — could be the artist or the customer)
- `UploadedByRole` (enum: `Artist`, `Customer`)
- `UploadedAt`
- `LinkedSessionDate` (datetime, nullable — when the actual tattoo session happened)
- `IsHidden` (bool — only artist can hide a customer-uploaded photo, only for content policy violations; admin-auditable)
- `HiddenReason` (string, nullable)

The `Fresh` photo for a piece is uploaded by the artist post-session. The `Healed` photo is uploaded by the customer 4 months after the booking via a prompted reminder. Both display paired on the portfolio piece.

## Bookings

### `BookingType` (enum)
`Consultation`, `TattooSession`, `Touchup`. All three exist in the schema; v1 only enables `TattooSession` in the UI but the others are scaffolded.

### `BookingStatus` (enum)
`Requested`, `AwaitingCustomerInfo`, `Accepted`, `DepositCaptured`, `Confirmed`, `InProgress`, `Completed`, `CancelledByArtist`, `CancelledByCustomer`, `Declined`, `Expired`.

State transitions:
- `Requested` → `AwaitingCustomerInfo` (artist requests more info) → `Requested` (customer responds)
- `Requested` → `Accepted` → `DepositCaptured` → `Confirmed` → `InProgress` → `Completed`
- `Requested` → `Declined` (artist declines)
- `Requested` → `Expired` (no response within 7 days, pre-auth voided)
- `Confirmed` → `CancelledByArtist` or `CancelledByCustomer` (deposit handled per cancellation policy)

The `Accepted` → `DepositCaptured` → `Confirmed` chain represents three distinct audit/telemetry moments. They typically occur within seconds of each other in normal flow, but are tracked separately because they involve a Stripe round-trip and may need to be debugged independently. `Accepted` is the moment the artist clicks Accept; `DepositCaptured` is set once Stripe confirms capture (synchronous response or `payment_intent.succeeded` webhook); `Confirmed` is the stable post-acceptance state at which the message thread opens and reminders schedule.

### `Booking`
- `Id`
- `CustomerId` (FK)
- `ArtistId` (FK)
- `StudioId` (FK — where the booking will take place; usually artist's primary, can differ for guest spots)
- `BookingType`
- `Status`
- `RequestedAt`
- `RequestedDate` (date — customer's preferred session date)
- `EstimatedDurationHours` (decimal)
- `Description` (text, max 5000 chars — what they want)
- `BodyPlacement` (enum)
- `ApproximateSizeCm` (int, optional)
- `Attachments` (collection of `BookingAttachment` — references images submitted by customer)
- `EstimatedTotalCad` (decimal, optional — artist's estimate for the work)
- `DepositAmountCad` (decimal — pre-authorized at request time)
- `StripePaymentIntentId` (string)
- `DepositCapturedAt` (datetime, nullable)
- `AcceptedAt` (nullable)
- `ConfirmedSessionDate` (datetime, nullable — set when artist accepts and confirms a specific date/time)
- `CompletedAt` (nullable)
- `CancellationPolicySnapshot` (enum — frozen at booking time so policy changes don't affect existing bookings)
- `DeclineReason` (enum: `NotAcceptingBookings`, `OutsideMyStyle`, `ScheduleConflict`, `Other`, nullable)
- `DeclineNote` (string, nullable)
- `MessageThread` (one-to-one with `MessageThread`, created on `DepositCaptured`)
- `Feedback` (one-to-one with `BookingFeedback`, created on `Completed`)
- `IsAttachmentsPurged` (bool — set when 1-year retention purge runs)

### `BookingAttachment`
A single dual-use attachment entity, used both for booking-request attachments and for in-thread message attachments. Exactly one of `BookingId` and `MessageId` is set per row (never both, never neither).
- `Id`
- `BookingId` (FK, nullable — set when the attachment is on a booking request)
- `MessageId` (FK, nullable — set when the attachment is on an in-thread message)
- `Url` (nullable — cleared when the underlying blob is purged)
- `OriginalFilename`
- `MimeType`
- `SizeBytes`
- `UploadedByUserId`
- `UploadedAt`

Retention: the underlying **blob** in object storage is purged 1 year after the related booking reaches a terminal state (Completed, Cancelled, Declined, Expired); the DB record is retained with `Url` cleared. Hangfire job runs nightly. Per ADR-003, **message text bodies are not purged** — only attachment blobs.

### `BookingFeedback`
Private, customer-to-Needlr only. Never shown to artist or other users.
- `Id`
- `BookingId` (FK, one-to-one)
- `CustomerId` (FK)
- `CommunicationRating` (1-5)
- `CleanlinessRating` (1-5)
- `RespectedDesignBriefRating` (1-5)
- `WouldBookAgain` (bool)
- `FreeText` (string, max 2000 chars, optional)
- `SubmittedAt`

Drives the admin trust & safety review queue. See `docs/FEATURE_SPECS.md` § Trust & Safety.

## Availability

### `AvailabilityPattern`
Recurring weekly availability.
- `Id`
- `ArtistId` (FK)
- `DayOfWeek`
- `Status` (enum: `Available`, `Limited`, `Closed`)
- `MaxSessionHours` (decimal, optional — null means no cap)
- `EffectiveFrom` (date)
- `EffectiveUntil` (date, nullable)

### `AvailabilityOverride`
One-off exceptions.
- `Id`
- `ArtistId` (FK)
- `Date`
- `Status` (enum: `Available`, `Limited`, `Closed`)
- `MaxSessionHours` (decimal, optional)
- `Reason` (string, optional — internal only)

### `BookingWindow`
Optional batching overlay. If any windows exist for an artist, requests are only accepted during open windows for sessions within the window's target range.
- `Id`
- `ArtistId` (FK)
- `WindowOpensAt` (datetime — when artist starts accepting requests)
- `WindowClosesAt` (datetime)
- `TargetRangeStart` (date — earliest session date this window accepts)
- `TargetRangeEnd` (date)

### `ArtistAvailabilityProjection`
Denormalized per-artist-per-day availability for fast filter queries.
- `Id`
- `ArtistId` (FK)
- `Date`
- `IsBookable` (bool — has open capacity given pattern + overrides + windows + existing bookings)
- `RemainingSessionHours` (decimal)
- `RecomputedAt`

Rebuilt by Hangfire nightly for a rolling 90-day window, plus on-demand on any change to underlying availability data for the affected artist.

## Messaging

### `MessageThread`
- `Id`
- `BookingId` (FK, one-to-one)
- `OpenedAt` (datetime — set when booking reaches `DepositCaptured`)
- `LockedAt` (datetime, nullable — set 90 days after booking reaches a terminal state)
- `Status` (enum: `Active`, `Locked`)

### `Message`
- `Id`
- `ThreadId` (FK)
- `SenderId` (FK to User)
- `Body` (text, max 5000 chars)
- `SentAt`
- `ReadAt` (nullable)
- `Attachments` (collection of `BookingAttachment`)
- `IsReportedFlag` (bool)

Messages cannot be edited or deleted by users. Admin can soft-delete in response to reports.

**Retention:** message text bodies are retained indefinitely (admin-only access after the thread locks). Only attachment blobs are purged at 1 year post-booking-terminal-state. See `docs/adr/ADR-003-message-privacy.md` § Retention for rationale.

### `MessageReport`
- `Id`
- `MessageId` (FK)
- `ReportedByUserId` (FK)
- `Reason` (enum: `Harassment`, `OffensiveContent`, `Spam`, `OffPlatformSolicitation`, `Other`)
- `Note` (string, optional)
- `ReportedAt`
- `ResolvedAt` (nullable)
- `ResolvedByAdminId` (nullable)
- `Resolution` (enum: `NoAction`, `MessageHidden`, `UserWarned`, `UserSuspended`, nullable)

## Other reference data

### `StudioHours`, `StudioCredential`, `ArtistCredential` — covered above.

### Enums summary
- `UserRole` — Customer, Artist, Admin
- `StudioType` — Shop, Solo, Private
- `JoinPolicy` — Open, InviteOnly, Closed
- `AffiliationRole` — Founder, Admin, Member
- `AffiliationType` — Permanent, GuestSpot
- `AffiliationStatus` — Pending, Active, Ended, Rejected
- `VerificationStatus` — Unverified, DocumentsSubmitted, Verified, Rejected, Expired
- `StudioCredentialType` — HealthInspection, MunicipalRegistration, Other
- `ArtistCredentialType` — BloodbornePathogenCertification, FormationHygieneEtSalubrite, LicensePractitioner, Other
- `BodyPlacement` — see above
- `ProgressionStatus` — SingleSession, MultiSessionInProgress, MultiSessionComplete
- `PhotoType` — Fresh, Healed
- `UploadedByRole` — Artist, Customer
- `BookingType` — Consultation, TattooSession, Touchup
- `BookingStatus` — see state transitions above
- `CancellationPolicy` — Strict, Standard, Flexible
- `DeclineReason` — NotAcceptingBookings, OutsideMyStyle, ScheduleConflict, Other
- `AvailabilityStatus` — Available, Limited, Closed
- `MessageThreadStatus` — Active, Locked
- `MessageReportReason` — Harassment, OffensiveContent, Spam, OffPlatformSolicitation, Other
- `MessageReportResolution` — NoAction, MessageHidden, UserWarned, UserSuspended
- `ArtistPaymentStatus` — NotOnboarded, OnboardingInProgress, Active, Restricted

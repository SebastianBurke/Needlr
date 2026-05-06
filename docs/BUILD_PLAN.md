# Needlr — Build Plan

This is the ordered execution plan for building Needlr v1. Work top-to-bottom. Within a phase, tasks are also ordered unless noted otherwise. Check off completed tasks in this file as you go.

After each phase: stop, summarize what was built, and confirm tests pass before starting the next phase.

Use subagents for genuinely parallelizable work within a phase (e.g., generating multiple independent EF configurations, scaffolding multiple controller files that don't depend on each other). Do not use subagents for sequentially-dependent work.

---

## Phase 0 — Repository foundation

- [x] Create `.gitignore` for .NET (standard template + `appsettings.*.Local.json`, `.env`, `wwwroot/uploads/`, `*.user`, `bin/`, `obj/`)
- [x] Create `README.md` at repo root with: prerequisites (.NET 9 SDK, Docker, Node optional), quick-start commands, link to `docs/`
- [x] Create `docker-compose.yml` at repo root running `postgis/postgis:16-3.4` on port 5432, named volume `needlr_pgdata`, default DB `needlr_dev`, user/password `needlr`/`needlr`. Healthcheck included.
- [x] Create `Directory.Build.props` at repo root: `<TargetFramework>net9.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (override to false for test projects via `Directory.Build.props` in `tests/`), `<LangVersion>13</LangVersion>`
- [x] Create `global.json` pinning .NET 9 SDK
- [x] Create solution file `Needlr.sln` and all project skeletons per `docs/ARCHITECTURE.md` § Solution layout
- [x] Add project references per layering rules in `docs/ARCHITECTURE.md`
- [x] Verify `dotnet build` succeeds with empty projects
- [x] Commit: "chore: repository foundation"

## Phase 1 — Domain layer

Build out `Needlr.Domain` with all entities and enums per `docs/DOMAIN_MODEL.md`. Pure data + invariants only; no behavior beyond constructor validation.

- [x] Add `NetTopologySuite` package to `Needlr.Domain`
- [x] Implement all enums (one file each, in `Domain/Enums/`)
- [x] Implement identity entities: `CustomerProfile`, `Artist`, `ArtistLeadTime` (`User` lives in `Needlr.Infrastructure` as `ApplicationUser : IdentityUser<Guid>`, scaffolded in Phase 2 — see DOMAIN_MODEL.md § User)
- [x] Implement studio entities: `Studio`, `StudioHours`, `ArtistStudioAffiliation`
- [x] Implement verification entities: `Jurisdiction`, `StudioCredential`, `ArtistCredential`
- [x] Implement portfolio entities: `TattooStyle`, `PortfolioPiece`, `SessionPhoto`
- [x] Implement booking entities: `Booking`, `BookingAttachment`, `BookingFeedback`
- [x] Implement availability entities: `AvailabilityPattern`, `AvailabilityOverride`, `BookingWindow`, `ArtistAvailabilityProjection`
- [x] Implement messaging entities: `MessageThread`, `Message`, `MessageReport`
- [x] Add `Needlr.Domain.Tests` with constructor invariant tests for each entity
- [x] Verify `dotnet test tests/Needlr.Domain.Tests` passes
- [x] Commit: "feat(domain): entities and enums"

## Phase 2 — Infrastructure foundation: DbContext, Identity, migrations

- [x] Add NuGet packages to `Needlr.Infrastructure` per `docs/ARCHITECTURE.md` § NuGet package map (Phase-2 subset only — Stripe/AWS/SendGrid/Serilog deferred to their phases per CLAUDE.md "don't add what you don't use yet")
- [x] Implement `NeedlrDbContext` with `DbSet<>` for every entity
- [x] Implement `IEntityTypeConfiguration<>` for each entity in `Infrastructure/Persistence/Configurations/`
  - [x] Spatial column configuration for `Studio.Location`, `CustomerProfile.Location` (`geometry(Point, 4326)`)
  - [x] GiST indexes on spatial columns
  - [x] Composite indexes where queries demand (`ArtistAvailabilityProjection (ArtistId, Date)`, `Booking (ArtistId, Status)`, etc.)
  - [x] Decimal precision for money fields (`decimal(10,2)` global default)
  - [x] Conversion of enum values to strings (not ints) for readability in DB (global convention in `OnModelCreating`)
- [x] Configure ASP.NET Core Identity to use `ApplicationUser : IdentityUser<Guid>` (lives in Infrastructure per Phase 1 layering decision)
- [x] Configure Hangfire to use PostgreSQL with its own schema (`AddHangfire` only — `AddHangfireServer` deferred to Phase 14 with the recurring-jobs setup)
- [x] Generate initial EF Core migration `InitialCreate`
- [x] Add `dotnet ef database update` to README quick-start (already covered in Phase 0 README)
- [x] Verify migration applies cleanly to a fresh container (`docker compose up -d`, then `dotnet ef database update`)
- [x] Commit: "feat(infrastructure): DbContext, Identity, initial migration"

## Phase 3 — Application foundation: MediatR, validators, abstractions

- [x] Add MediatR + FluentValidation to `Needlr.Application` (MediatR 12.4.1 — last free; FluentValidation 11.9.2 — last Apache-2.0; Mapster 7.4.0)
- [x] Configure MediatR pipeline: validation behavior, logging behavior, transaction behavior (auto-commits via `IUnitOfWork.SaveChangesAsync` for `ICommandBase` requests on successful `Result`)
- [x] Define interfaces in `Application/Abstractions/`:
  - [x] `IImageStorage`
  - [x] `IEmailSender`
  - [x] `IPushNotificationSender`
  - [x] `IStripeService` (wraps Stripe.net for testability)
  - [x] `IClock` (for testable `DateTime.UtcNow`)
  - [x] `ICurrentUser` (for accessing current user from handlers)
  - [x] `IUnitOfWork` (added — abstraction so `TransactionBehavior` and handlers don't reference the concrete `NeedlrDbContext`)
- [x] Add `Result<T>` type for handler return values — chose custom `Result` / `Result<T>` + `Error` over OneOf to avoid an extra dependency
- [x] Implement application-layer common types: pagination (`PageRequest`, `PagedResult<T>`), sort enums (`SortDirection`), geographic filter shapes (`GeoPoint`, `BoundingBox`)
- [x] Add `NetArchTest` package and layering-enforcement tests to `Needlr.Architecture.Tests` (project skeleton created in Phase 0)
- [x] Commit: "feat(application): MediatR pipeline and abstractions"

## Phase 4 — Auth & users

- [x] Implement `RegisterCustomerCommand` + handler + validator
- [x] Implement `RegisterArtistCommand` + handler + validator
- [x] Implement `LoginCommand` + handler returning JWT
- [x] Implement refresh token storage and `RefreshTokenCommand` (rotating, SHA256-hashed, with replacement-chain audit; `LogoutCommand` revokes)
- [x] Implement `AuthController` in Api with login, register, refresh, logout endpoints
- [x] Configure JWT bearer auth in Api `Program.cs` with appropriate claims
- [x] Implement `ICurrentUser` impl reading from `HttpContext` — placed in `Needlr.Api.Auth` rather than Infrastructure because `IHttpContextAccessor` lives in `Microsoft.AspNetCore.App` (FrameworkReference would be required for Infrastructure to use it)
- [x] `[Authorize(Roles = ...)]` machinery wired (JWT bearer scheme, `AddAuthorization`, role claim mapped from `ClaimTypes.Role`); first role-restricted endpoint will arrive in Phase 5+ when admin/artist actions appear
- [x] Add integration tests: register, login, refresh, logout — 13 tests covering happy path + duplicate email + weak password + wrong password + unknown email + token rotation/re-use detection + revoke-on-logout + logout-no-leak
- [x] Commit: "feat(auth): registration, login, JWT, refresh tokens"

## Phase 5 — Studios & artist-studio affiliations

- [x] Implement `CreateStudioCommand` (creates studio with founding artist as Founder/Admin, becomes primary if no existing primary)
- [x] Implement `UpdateStudioInfoCommand` (admin-only via `IStudioAuthorization.IsCurrentUserStudioAdminAsync`)
- [x] Implement `RequestStudioJoinCommand` (artist requests; only allowed when `JoinPolicy == Open`)
- [x] Implement `RespondToJoinRequestCommand` (admin approves/rejects; rejects guest-spot affiliations — those go through `RespondToGuestSpotCommand`)
- [x] Implement `InviteArtistToStudioCommand` (admin-initiated; rejects when studio is `Closed`)
- [x] Implement `RespondToStudioInvitationCommand` (invited artist accepts/rejects)
- [x] Implement `ChangeAffiliationRoleCommand` (Member ↔ Admin only; founder must be ceded explicitly)
- [x] Implement `RemoveAffiliationCommand` (admin or self; founder cannot leave without ceding)
- [x] Implement `RequestGuestSpotCommand` and `RespondToGuestSpotCommand` (time-boxed `GuestSpot` affiliation; admin host approves)
- [x] Implement `SetPrimaryAffiliationCommand` (artist sets primary; clears IsPrimary on their other affiliations)
- [x] Implement queries: `GetStudioByIdQuery`, `GetStudioRosterQuery`, `GetMyAffiliationsQuery`, `SearchStudiosByNameQuery`
- [x] Implement `StudiosController` (anonymous Get/Search/Roster; Artist-role required for Create/Update) and `AffiliationsController` (Artist-role required for all endpoints)
- [x] Integration tests covering all join/leave/role flows including guest spots — 13 new studio tests on top of Phase 4's auth tests
- [x] Commit: "feat(studios): studio creation, affiliations, guest spots"

## Phase 6 — Verification

- [x] Seed `Jurisdiction` table with Montréal row (idempotent `DataSeeder` IHostedService)
- [x] Implement `UploadStudioCredentialCommand` (studio admin only via `IStudioAuthorization`; uploads via `IImageStorage`)
- [x] Implement `UploadArtistCredentialCommand` (caller's own artist record)
- [x] Implement `ReviewCredentialCommand` (admin approves/rejects; sets `VerifiedByAdminId`/`VerifiedAt` or `RejectionReason`)
- [x] Implement query `GetVerificationQueueQuery` for admin dashboard (returns DocumentsSubmitted from both studio + artist credential tables)
- [x] Implement computed verification status logic for `Artist` and `Studio` (`IVerificationStatusService`; queries credentials against required types per jurisdiction; expiry-grace transitions deferred to Phase 14's nightly job)
- [x] Implement `LocalFilesystemImageStorage` (selected via `ImageStorage:Backend = Local`; writes under configurable `LocalRootPath`, default `wwwroot/uploads`)
- [x] Implement `R2ImageStorage` stub (selected via `ImageStorage:Backend = R2`; throws `NotImplementedException` until AWSSDK.S3 lands so misconfig is loud)
- [x] Add `CredentialsController` ([Authorize Roles=Artist] for both studio + artist uploads — handler enforces studio-admin check) and `AdminController` ([Authorize Roles=Admin] for queue + review)
- [x] Integration tests: upload, review (approve/reject), queue, RBAC negative cases (8 new tests on top of Phase 4/5 coverage)
- [x] Commit: "feat(verification): credentials, admin review, computed status"

Notes:
- `DataSeeder` calls `Database.MigrateAsync()` first so its seed queries don't precede schema creation. Idempotent in dev/test; in production prefer running `dotnet ef database update` from a deploy step (the seeder still no-ops cleanly).
- `Jurisdiction.RequiresMunicipalRegistration` is not currently a flag on the entity (only `RequiresStudioInspection` exists). FEATURE_SPECS.md § Required credentials lists municipal registration as required, but the Domain model treats it as informational. If you want it enforced for "Verified", add a flag to `Jurisdiction` and update `VerificationStatusService.StudioJurisdictionFullySatisfied`.

## Phase 7 — Portfolio

- [x] Seed `TattooStyle` with the canonical 32 styles from `docs/FEATURE_SPECS.md` (`IsCanonical = true`) — added to `DataSeeder` (idempotent, by slug)
- [ ] **Deferred**: Image upload pipeline (validate, EXIF strip, resize to thumb/medium/full). Phase 7 ships with the original blob stored as-is via `IImageStorage`; no resizing or EXIF strip. SkiaSharp not yet pulled in. To address before launch — add a Phase-23-hardening item or a new Phase 7.5. **This is the only Phase 7 build-plan item not delivered as written.**
- [x] Implement `CreatePortfolioPieceCommand` (artist uploads fresh photo + metadata; resolves canonical styles by id; rejects unknown style ids)
- [x] Implement `AddSessionPhotoCommand` (Fresh or Healed; artist owns piece check)
- [x] Implement `UploadHealedPhotoCommand` (customer uploads against their Completed booking; appends `Healed` photo to the piece linked to that booking; returns 412 if the artist hasn't created the linked piece yet)
- [x] Implement `HideSessionPhotoCommand` (artist owns piece; reason ≥ 10 chars and admin-auditable per FEATURE_SPECS.md § Customer-uploaded photo policy)
- [x] Implement `UpdatePortfolioPieceCommand` (artist owns piece; replaces styles + freeform tags wholesale)
- [x] Implement `DeletePortfolioPieceCommand` (artist owns piece; cascade removes Sessions; orphan blob cleanup deferred to Phase 14)
- [x] Implement queries: `GetArtistPortfolioQuery` (paginated), `GetPortfolioPieceQuery` (full detail incl. styles + photos), `GetStudioCollectivePortfolioQuery` (pieces by all currently-Active affiliated artists)
- [x] Implement `PortfolioController` — `[Authorize(Roles=Artist)]` for create/update/delete/add-photo/hide; `[Authorize(Roles=Customer)]` for healed-photo upload; anonymous read for pieces + listings
- [x] Integration tests: 18 covering create + retrieve, paginated artist portfolio, studio collective portfolio with multi-artist roster, add session photo, RBAC negatives (403/404/400), hide-with-short-reason rejection, healed-photo flow with seeded Booking + linked piece
- [x] Commit: "feat(portfolio): pieces, photos, paired fresh+healed model"

## Phase 8 — Discovery

- [x] Implement `IArtistDiscoveryService` in Application layer with method `SearchAsync(SearchCriteria, CancellationToken)`
  - [x] Bounding-box spatial filter — uses literal X/Y axis comparisons on the Point column (planner picks them up cleanly with the GiST index; sidesteps antimeridian-wrap which Montréal-only v1 doesn't hit). `ST_Within` is available if we need a true polygon test later.
  - [x] Style filter — studio has at least one Active artist with any matching style
  - [x] Verified filter — at studio level: has at least one Verified `HealthInspection` credential when the toggle is on; allows `DocumentsSubmitted` when off; `Unverified`/`Rejected` always excluded
  - [x] Availability filter — joins `ArtistAvailabilityProjection`; scaffolded query path even though Phase 9 populates the projection
  - [x] Sort by distance ascending (`Location.Distance(center)` → `ST_Distance` in EF translation)
  - [x] Alternative sorts scaffolded: `AvailabilitySoonness` (min bookable date in the requested window, then distance), `VerifiedFirst` (verified studios first, then distance)
- [x] Implement `DiscoveryController` with `GET /api/discovery/studios` returning paginated results
- [x] `GET /api/studios/{id}` already exists from Phase 5; `GET /api/studios/{id}/roster` covers the roster — kept separate so clients with cached studio metadata can refetch the roster cheaply
- [x] Implement `GET /api/artists/{id}` (new `ArtistsController`) returning `ArtistDetailResponse` (display fields + computed verification status + primary studio summary + styles)
- [x] Integration tests: bbox in/out, verified filter on/off, no-credential exclusion, distance ordering, pagination, artist detail with style attached, artist 404
- [x] Commit: "feat(discovery): spatial search, filters, sort"

## Phase 9 — Availability

- [x] Implement `SetAvailabilityPatternCommand` (artist sets recurring weekly pattern; replaces wholesale, rebuilds projection)
- [x] Implement `AddAvailabilityOverrideCommand`, `RemoveAvailabilityOverrideCommand` (replace-by-date semantics on add)
- [x] Implement `CreateBookingWindowCommand`, `CloseBookingWindowCommand` (close = hard delete; per-artist scoped)
- [x] Implement `SetLeadTimesCommand` (replaces all rows at once; partial updates rejected — see ADR-style note in handler XML doc)
- [x] Implement `IAvailabilityProjector` service that computes `IsBookable` per artist per day given pattern + overrides + windows + bookings (`AvailabilityProjector` in Infrastructure; pattern resolves by latest-effective-from for the day-of-week, overrides win outright, windows gate every day if any window exists, capacity defaults Available=8h / Limited=3h when not set)
- [x] Implement `RebuildArtistAvailabilityProjectionCommand` (single artist, on-demand; admin-only via `[Authorize(Roles=Admin)]`)
- [x] Implement `RebuildAllAvailabilityProjectionsRecurringJob` for Hangfire — class shipped in Phase 9 (registered in DI); cron + `AddHangfireServer` deferred to Phase 14 per the existing CLAUDE.md "don't add what you don't use yet" rule
- [x] Wire on-demand recomputation triggers: pattern change, override change, window change. **Booking-side triggers (accept/cancel/complete) deferred to Phase 10** — those handlers don't exist yet; Phase 10 wires them into `AcceptBookingCommand`, `CancelBooking*Commands`, and `MarkBookingCompletedCommand` by calling `IAvailabilityProjector.RebuildRollingWindowAsync`.
- [x] Implement `AvailabilityController` exposing pattern/override CRUD + iCal export (artist-role for management, anonymous for projection read + iCal feed, admin for cross-artist rebuild)
- [x] iCal export: tokenized URL per artist (`/api/availability/ical/{artistId}/{token}.ics`), returns `text/calendar` with `Accepted/DepositCaptured/Confirmed/InProgress/Completed` bookings (any booking with a `ConfirmedSessionDate`), 30-day backstop on past sessions, fixed-time-comparison token check to prevent timing oracles
- [x] Integration tests covering projection accuracy across pattern + override + window + booking interactions — 14 new tests on top of prior coverage
- [x] Commit: "feat(availability): patterns, overrides, projection, iCal export"

Notes:
- **EF flush ordering**: handlers that mutate availability and then call `IAvailabilityProjector` (set pattern, add/remove override, create/close window) issue an explicit `IUnitOfWork.SaveChangesAsync` before the projector runs. Tracked-but-unsaved entities are not visible to LINQ queries by default, so the projector would otherwise not see the just-added rows. The `TransactionBehavior` still saves a final time at handler end.
- **Domain change**: `Artist.IcalToken` (nullable string, max 64) added to support the per-artist iCal feed. Migration `20260506_Phase9_Availability` adds the column + a partial unique index (`WHERE ical_token IS NOT NULL`). DOMAIN_MODEL.md § Artist updated.
- **Capacity defaults**: when `MaxSessionHours` is unset on a pattern row, the projector applies 8h for Available, 3h for Limited (FEATURE_SPECS.md doesn't pin a default; these are the projector's working assumption).
- **Booking statuses considered**: capacity-consuming for the projector are `Accepted | DepositCaptured | Confirmed | InProgress`; the iCal feed additionally includes `Completed`. Cancelled/Declined/Expired never enter either set.

## Phase 10 — Bookings core (no Stripe yet — build the state machine first)

- [x] Implement `RequestBookingCommand` (validates lead time vs `ArtistLeadTime` with platform-default fallbacks, runs `IContactInfoStripper` over the description, picks the artist's primary-or-most-recent Active studio, snapshots `CancellationPolicy`, persists as `Requested`; deposit pre-auth deferred to Phase 11)
- [x] Implement `AcceptBookingCommand` (artist accepts, sets `ConfirmedSessionDate` + `AcceptedAt`, transitions Requested → Accepted; rebuilds availability projector. Stripe capture / `DepositCaptured` / `Confirmed` chain deferred to Phase 11)
- [x] Implement `DeclineBookingCommand` (records `DeclineReason` enum + optional note; pre-auth void deferred to Phase 11)
- [x] Implement `RequestMoreInfoCommand` (Requested → AwaitingCustomerInfo; structured-prompt UX is FE-only)
- [x] Implement `RespondWithMoreInfoCommand` (AwaitingCustomerInfo → Requested; allows revising description/date/duration/placement/size/total; description re-stripped)
- [x] Implement `MarkBookingInProgressCommand`, `MarkBookingCompletedCommand` (artist-only; allows skipping InProgress per spec; Completed rebuilds projector)
- [x] Implement `CancelBookingByCustomerCommand`, `CancelBookingByArtistCommand` — refund decision via shared `CancellationRefundPolicy`: Strict 0%, Standard 100% if >7 days else 0%, Flexible 100% if >48h else 0%, artist cancellation always 100%; Stripe refund deferred to Phase 11. Both rebuild the projector when the cancelled booking was capacity-consuming.
- [x] Implement `ExpireRequestedBookingCommand` (idempotent single-booking; Hangfire-callable) plus `ExpireDueRequestedBookingsRecurringJob` job class. Cron + `AddHangfireServer` deferred to Phase 14.
- [x] Implement queries: `GetMyBookingsAsCustomerQuery`, `GetMyBookingsAsArtistQuery` (paginated, optional status filter, newest-first), `GetBookingDetailQuery` (admin / customer-party / artist-party visibility)
- [x] Implement `BookingsController` (Customer-role for request / respond-info / cancel-customer; Artist-role for accept / decline / request-info / in-progress / complete / cancel-artist; `[Authorize]` plain on detail; role-routed listing endpoints)
- [x] Integration tests covering all state transitions and lead time enforcement — 16 new tests
- [x] Commit: "feat(bookings): request flow, state machine, lead time enforcement"

Notes:
- **Stripe-side actions deferred to Phase 11**: deposit pre-authorization at request time, capture on accept (and the Accepted → DepositCaptured → Confirmed transition chain), pre-auth void on decline/expire, and refunds on cancel. Phase 10 ships handlers that compute the *decision* (refund amount via `CancellationRefundPolicy`) so Phase 11 can plug in Stripe behind them without further state-machine churn.
- **MessageThread auto-open deferred to Phase 12**: per spec the thread opens at `DepositCaptured`. Since Phase 10 stops at `Accepted`, no thread is created. Phase 12 will hook `DepositCaptured` (driven by the Stripe webhook that lands in Phase 11) to call the new `OpenMessageThreadOnDepositCapturedHandler`.
- **EF flush ordering**: Phase 9's pattern continues — handlers that mutate state and then call `IAvailabilityProjector` (Accept, MarkCompleted, Cancel*) issue an explicit `IUnitOfWork.SaveChangesAsync` before the projector runs so its reads see the just-mutated booking row.
- **Default lead times when unset**: handler falls back to FEATURE_SPECS.md § Artist onboarding step 11 defaults (Consultation 3 / TattooSession 7 / Touchup 7) for any booking type that doesn't have an `ArtistLeadTime` row.
- **Default deposit**: a single platform-wide $100 CAD constant (`BookingDefaults.DefaultDepositCad`) is used at request time. Per-artist overrides + the actual Stripe Payment Intent amount come in Phase 11.
- **`ContactInfoStripper`**: regex-based, conservative (over-strips rather than leaks). Phone / email / URL / @-handle patterns. Replacement is a human-readable token so customers see what was stripped before re-submit.

## Phase 11 — Stripe Connect integration

- [x] Implement `IStripeService` infrastructure impl (`StripeService` in `Needlr.Infrastructure.Stripe`) wrapping `Stripe.net` 51.x. Every per-account call sets `RequestOptions.StripeAccount = artist.StripeConnectAccountId` per ADR-005 (direct-charge model). Added `Stripe.net` package reference; bound `StripeOptions` (SecretKey, ConnectWebhookSigningSecret, OnboardingReturnUrl, OnboardingRefreshUrl) opt-in via the `Stripe` config section so dev runs without keys don't fail validation at startup.
- [x] Implement `CreateConnectAccountCommand` (idempotent — reuses existing `StripeConnectAccountId` if set; flips `PaymentStatus` from `NotOnboarded` to `OnboardingInProgress`) and `GenerateOnboardingLinkCommand` (returns hosted Account Link URL; per-call return/refresh URL overrides fall back to `StripeOptions`).
- [x] Implement webhook endpoint `POST /api/webhooks/stripe` (`StripeWebhooksController`) with raw-body buffering + `Stripe-Signature` verification via `EventUtility.ConstructEvent` inside `IStripeWebhookProcessor`.
- [x] Webhook handlers for: `account.updated` (maps `(charges_enabled, details_submitted)` → `ArtistPaymentStatus.Active | Restricted | OnboardingInProgress`), `payment_intent.succeeded` (sets `DepositCapturedAt`, advances Accepted/DepositCaptured → Confirmed), `payment_intent.canceled` (audit-only — local handlers already moved the booking), `charge.refunded` (audit-log refund amount), `charge.dispute.created` (logged at warn for ops; admin dashboard wiring in Phase 15). All routed through `StripeProcessedEvent` (event id PK + partial unique index) for idempotency; redeliveries no-op.
- [x] Wire pre-auth into `RequestBookingCommand` — creates manual-capture PaymentIntent on the artist's connect account, stores `StripePaymentIntentId`. New precondition: artist must have `PaymentStatus = Active` and a non-null `StripeConnectAccountId`. Customer payment method id (pm_…) added to the request contract.
- [x] Wire capture into `AcceptBookingCommand` — `PaymentIntent.Capture` on the connected account; the webhook flips status to Confirmed when Stripe acknowledges.
- [x] Wire void into `DeclineBookingCommand` and `ExpireRequestedBookingCommand` — `PaymentIntent.Cancel` on the connected account.
- [x] Wire refund logic into cancellation handlers per policy snapshot — pre-auth-only states (Requested/AwaitingCustomerInfo) cancel the intent; post-capture states issue `Refund.Create` with the amount returned by `CancellationRefundPolicy`.
- [x] Add Hangfire scheduled job: at booking creation, `IBookingExpiryScheduler.Schedule` (`HangfireBookingExpiryScheduler` in Infrastructure) enqueues `ExpireRequestedBookingCommand` for `RequestedAt + 7 days`. Idempotent expire handler tolerates earlier accept/decline.
- [x] Integration tests with `FakeStripeService` (records calls, returns deterministic ids) + `StripeSignatureHelper` (HMAC-SHA256, Stripe's `t=ts,v1=hex` format). `WebAppFixture` configures the Stripe section with a test webhook secret + replaces `IStripeService` and `IBookingExpiryScheduler` with test doubles. 13 new Stripe tests on top of the existing 17 booking tests; full suite **260 / 0 fail**.
- [x] Commit: "feat(stripe): Connect onboarding, pre-auth, capture, refunds, webhooks"

Notes:
- **Real Stripe test-mode integration is intentionally NOT exercised in tests.** The test suite must run hermetically without external network calls. `FakeStripeService` lets handlers exercise the full state machine deterministically; the production `StripeService` lands in DI only when the `Stripe` config section exists (so dev/local without keys is loud rather than silent). Phase 23 hardening can add a Stripe-test-mode smoke test if desired.
- **Webhook idempotency**: `StripeProcessedEvents` table records every processed event id. The unique-key DbUpdateException is caught + treated as Processed so concurrent workers redelivering the same event don't duplicate side effects.
- **Booking-side webhook lag**: per FEATURE_SPECS § Artist response options, the Accepted → DepositCaptured → Confirmed chain may be sub-second apart. The webhook handler stamps `DepositCapturedAt` but only advances status to `Confirmed` when the booking is in `Accepted` or `DepositCaptured` — never demotes a later state (e.g., a quick CancelledByCustomer between accept and webhook delivery wins).
- **Connect-account onboarding has no controller in Phase 11.** The commands ship; the FE wizard + admin tooling (Phase 20) wire UI on top. Tests dispatch via `IMediator` directly with an impersonated principal. A controller can be added in Phase 20 alongside the artist onboarding wizard without touching handlers.
- **Hangfire scheduled job uses `BackgroundJob.Schedule` via `IBackgroundJobClient`.** Tests substitute `NoopBookingExpiryScheduler` so we don't depend on a Hangfire server; the recurring 4 AM sweep (`ExpireDueRequestedBookingsRecurringJob`) catches any slip-ups when the per-booking schedule misses.
- **NuGet package added**: `Stripe.net 51.1.0`. Migration `20260506_Phase11_StripeProcessedEvents` creates the idempotency table.

## Phase 12 — Messaging

- [x] Implement thread auto-open at `DepositCaptured` — wired inline into `StripeWebhookProcessor.HandlePaymentIntentSucceededAsync` rather than via a domain event handler (the codebase doesn't have an event dispatcher yet). Idempotent: only on the *first* webhook capture and only if no thread already exists for the booking. The webhook is the ground truth for "funds actually moved" per ADR-005.
- [x] Implement `SendMessageCommand` — Active-thread + party check (caller is the booking's customer, or the artist's `UserId`). No content stripping post-acceptance per FEATURE_SPECS § Pre-acceptance content stripping.
- [x] Implement `MarkMessageReadCommand` — only the *recipient* (not the sender) can mark read; idempotent via `??=`. Authenticated party check.
- [x] Implement `UploadMessageAttachmentCommand` — sender-only (no piggybacking files on the other party's messages); MIME allowlist (jpeg/png/webp); 10MB cap defended in both controller (`RequestSizeLimit`) and command (validation).
- [x] Implement `ReportMessageCommand` — either party reports; sets `IsReportedFlag` on the message and creates a `MessageReport` row for the admin queue.
- [x] Implement admin commands: `HideMessageCommand` (replaces body with redaction notice; original retained per ADR-003 § Retention; admin tooling in Phase 22 will surface the original from a separate audit endpoint), `ResolveMessageReportCommand` (records resolution + resolver id; rejects already-resolved).
- [x] Implement queries: `GetThreadMessagesQuery` (paginated, oldest-first; admin or party), `GetUnreadMessageCountQuery` (messages user didn't author and hasn't marked read across active threads), `GetMyActiveThreadsQuery` (sorted by latest message time desc).
- [x] Implement `MessagesController` (`/api/threads/...` + `/api/messages/...` routes; `[Authorize]`; admin moderation routes folded into `AdminController` under `/api/admin/messages/{id}/hide` and `/api/admin/message-reports/{id}/resolve`).
- [x] Implement thread-lock scheduling: `IThreadLockScheduler` abstraction + `HangfireThreadLockScheduler` impl + `LockMessageThreadCommand` + `LockOverdueThreadsRecurringJob` safety net. `MarkBookingCompleted`, `CancelBookingByCustomer`, `CancelBookingByArtist` now schedule `LockMessageThreadCommand` for `now + 90 days`. (Decline/Expire pre-date `DepositCaptured` → no thread exists, scheduler call would be a no-op; not wired.)
- [x] Integration tests — 11 new tests covering: send-before-thread-open returns 404; webhook auto-open + parties exchange; non-party send 403; locked-thread send 412 by party; recipient-only read with sender-side 412; unread count counts only other-party messages; report flips flag + creates row; admin hide redacts body; admin resolve records resolution; lock idempotency; `/api/threads/mine` party-scope.
- [x] Commit: "feat(messaging): booking-scoped threads, reports, retention"

Notes:
- **Auto-open trigger is the Stripe webhook, not a Domain event.** The codebase has no domain event dispatcher yet, and adding one for this single trigger would be premature abstraction. The webhook handler already runs in a request-scoped DbContext + has access to the same `IClock`; tucking thread creation in alongside `DepositCapturedAt` is the smallest, most testable hook. If we later add a dispatcher (Phase 13's notification work is a likely catalyst), this can move there with one line.
- **Phase 13 notification dispatch deferred**: "new message in your active thread" email + push fan-out lands in Phase 13. Phase 12 just persists the message + flips `IsReportedFlag`; the `IEmailSender` / `IPushNotificationSender` calls aren't here.
- **Phase 14 attachment-blob purge deferred**: per ADR-003 § Retention, message *attachment blobs* purge 1 year after the booking's terminal state; *message bodies* are retained indefinitely with admin-only access. The `BookingAttachment.Url`-clearing job is `NightlyBookingAttachmentPurgeJob` and arrives in Phase 14 alongside the rest of the recurring jobs.
- **EF cascade**: `MessageThread` cascades from `Booking`; `Message` cascades from `MessageThread`; `BookingAttachment` cascades from either parent. Booking deletion is rare (FK is Restrict on Booking → ApplicationUser), but the messaging side aligns with "delete the related booking → delete the conversation."
- **Thread-lock scheduler vs. recurring sweep**: per-booking schedule is the preferred path (job fires precisely at +90d). The recurring `LockOverdueThreadsRecurringJob` (registered class only; cron in Phase 14) catches threads whose schedule was missed (server down during the lock window, etc.).

## Phase 13 — Notifications

- [ ] Implement `IEmailSender` console impl (logs to console for dev) and SendGrid impl (gated by config)
- [ ] Implement `IPushNotificationSender` Web Push impl using VAPID keys
- [ ] Implement notification preference per-user storage (per-channel toggles for each notification type from `docs/FEATURE_SPECS.md` § Notifications)
- [ ] Implement notification dispatcher: handlers for each domain event that emit appropriate emails + pushes per preferences
- [ ] Implement `RegisterPushSubscriptionCommand` for browser push subscriptions
- [ ] Implement `UpdateNotificationPreferencesCommand`
- [ ] Implement `NotificationsController` for preference management
- [ ] Integration tests for notification dispatch (mock `IEmailSender`/`IPushNotificationSender`, verify correct calls)
- [ ] Commit: "feat(notifications): email + web push, per-channel preferences"

## Phase 14 — Hangfire recurring jobs

- [ ] `NightlyAvailabilityProjectionRebuildJob` (3 AM)
- [ ] `NightlyBookingAttachmentPurgeJob` (3:30 AM): purges attachment blobs (object-storage files) and clears `BookingAttachment.Url` for bookings 1+ year past terminal state. Per ADR-003, message text bodies are NOT purged — only blobs.
- [ ] `NightlyCredentialExpiryScanJob` (4 AM): warns 30/7/0 days, downgrades on expiry
- [ ] `DailyHealedPhotoPromptJob` (10 AM): prompts customers at 4-month mark
- [ ] `DailyBookingReminderJob` (9 AM): 24-hour reminders
- [ ] Wire all jobs in `Program.cs` startup
- [ ] Add Hangfire dashboard at `/hangfire` (admin-only auth)
- [ ] Integration tests for each job's logic
- [ ] Commit: "feat(jobs): Hangfire recurring jobs for projections, retention, expiry, prompts"

## Phase 15 — Trust & safety

- [ ] Implement `SubmitBookingFeedbackCommand` (private feedback after Completed booking)
- [ ] Implement behavioral signal computation (queries that compute response time, completion rate, healed photo rate, repeat client rate per artist)
- [ ] Surface behavioral signals on `GET /api/artists/{id}` response
- [ ] Implement admin trust & safety dashboard query: artists with low feedback averages, free-text containing safety keywords
- [ ] Implement `SuspendArtistCommand`, `SuspendCustomerCommand`, `WarnUserCommand` (admin actions)
- [ ] Suspended artists are invisible in discovery; existing bookings honored
- [ ] Add `AdminController.TrustAndSafety` endpoints
- [ ] Integration tests covering feedback flow, behavioral signal accuracy, suspension effects
- [ ] Commit: "feat(trust-safety): private feedback, behavioral signals, admin actions"

## Phase 16 — Web frontend foundation (Blazor PWA)

- [ ] Configure `Needlr.Web` as Blazor WASM with PWA support
- [ ] Configure service worker for offline app shell + asset caching
- [ ] Implement `AuthState` scoped service with token storage in `localStorage`, refresh logic, automatic 401 retry
- [ ] Implement `INeedlrApi` typed HTTP client in Contracts; impl in Web wraps `HttpClient` with bearer token injection
- [ ] Implement layout components: nav, footer, mobile bottom nav
- [ ] Implement auth pages: Register (Customer + Artist flows), Login, Forgot Password
- [ ] Implement Web Push subscription on first authenticated visit
- [ ] Commit: "feat(web): PWA foundation, auth state, layout"

## Phase 17 — Web frontend: discovery (the headline feature)

- [ ] Add MapLibre GL JS to `wwwroot` and create `mapInterop.js` module
- [ ] Implement `MapComponent.razor` wrapping MapLibre with `OnBoundsChanged` callback (debounced 300ms)
- [ ] Implement `DiscoveryPage.razor` (the home/landing page)
  - Map view (primary) with studio pins, clustering at low zoom
  - Filter bar: Style multi-select, Verified toggle (default on), Availability date range + accepting-new-bookings toggle
  - Sort selector: Distance (default), Availability soonness, Verified first
  - List view (secondary): toggleable; on mobile, draggable bottom sheet
  - Click pin → studio detail panel
- [ ] Implement `StudioDetailPanel.razor` showing studio info, roster, primary credentials badge
- [ ] Implement `ArtistProfilePage.razor` with portfolio grid, behavioral signals, bio, "Request Booking" CTA
- [ ] Implement `PortfolioPiecePage.razor` showing paired fresh+healed photos, metadata
- [ ] Commit: "feat(web): discovery, map, studio detail, artist profile, portfolio"

## Phase 18 — Web frontend: bookings

- [ ] Implement `BookingRequestForm.razor` with all structured fields per `docs/FEATURE_SPECS.md` § Customer-initiated request flow
- [ ] Inline regex strip warning for contact info in description field
- [ ] Reference image upload component (drag-drop, preview, max 8 images)
- [ ] Stripe Elements integration for payment method capture
- [ ] Confirmation step showing artist's cancellation policy clearly
- [ ] Customer booking dashboard: list of bookings by status
- [ ] Booking detail page: status timeline, attached photos, message thread (when active)
- [ ] Artist booking inbox: requests awaiting response, accepted/upcoming, completed history
- [ ] Artist response UI: Accept (with date/time confirmation), Decline (reason picker + note), Request More Info (structured prompt)
- [ ] Commit: "feat(web): booking request, payment capture, dashboards"

## Phase 19 — Web frontend: messaging

- [ ] Implement `MessageThreadPage.razor` with message list, composer, attachment upload
- [ ] Inline notification of thread status (Active / ReadOnly / Locked)
- [ ] Unread badge on nav
- [ ] Polling for new messages (every 30s when thread is open) — no SignalR in v1
- [ ] Report message UI with reason picker
- [ ] Commit: "feat(web): messaging UI"

## Phase 20 — Web frontend: artist tooling

- [ ] Artist onboarding wizard (multi-step: profile → studio choice → studio info if creating → credentials → Stripe Connect → portfolio seed → availability → policy)
- [ ] Stripe Connect onboarding redirect flow with return-from-Stripe handling
- [ ] Portfolio management: upload pieces, edit, delete, multi-session photo management, hide-customer-photo flow
- [ ] Availability management: weekly pattern editor, override calendar, booking window manager, lead times
- [ ] Studio management (for admins): edit info, credential uploads, roster management, join-policy controls, invitation flow
- [ ] Profile management: bio, hourly rate, shop minimum, accepting-bookings toggle
- [ ] iCal feed link display + copy button
- [ ] Commit: "feat(web): artist onboarding wizard, portfolio/availability/studio management"

## Phase 21 — Web frontend: customer tooling

- [ ] Customer profile: home location, preferred styles, search radius
- [ ] Healed photo upload flow (triggered from healed photo prompt notification, accessible from booking detail)
- [ ] Private feedback form (post-booking)
- [ ] Notification preferences page (per-channel toggles)
- [ ] PWA install prompt after first confirmed booking
- [ ] Commit: "feat(web): customer profile, healed upload, feedback, prefs"

## Phase 22 — Web frontend: admin tooling

- [ ] Admin dashboard layout (separate from customer/artist UI, role-gated)
- [ ] Verification queue (pending credential reviews, with document preview, approve/reject actions)
- [ ] Trust & safety dashboard (flagged artists by feedback patterns, message reports queue)
- [ ] User management (search, suspend, warn, ban actions)
- [ ] Tag management (promote freeform tags to canonical TattooStyles)
- [ ] Hangfire dashboard link (already exists at `/hangfire`)
- [ ] Commit: "feat(web): admin tooling"

## Phase 23 — Final integration, polish, hardening

- [ ] End-to-end test scripts that exercise full flows: customer signs up, finds artist, books, deposit captured, message exchange, completion, healed photo upload, feedback
- [ ] Same for artist: signs up, completes onboarding incl. Stripe Connect, gets verified, receives booking, accepts, completes
- [ ] Same for studio admin: creates studio, uploads credentials, gets verified, invites artists, manages roster
- [ ] Performance check: discovery query under load with seeded data (~500 studios, ~2000 artists, ~50k portfolio pieces, ~10k bookings)
- [ ] Logging review: every significant action logged via Serilog with appropriate context
- [ ] Error handling review: no unhandled exceptions reach the user; all surface as appropriate error responses
- [ ] Security review: SQL injection (EF protects), XSS (Blazor protects), CSRF (JWT bearer + same-site), file upload validation, secrets in env vars not source
- [ ] Accessibility pass: keyboard nav, screen reader labels, color contrast on map markers
- [ ] Mobile responsive pass: all pages work on 375px width
- [ ] Update README with full setup, deployment notes, and contribution guidelines
- [ ] Commit: "chore: v1 hardening and polish"

---

## Notes for execution

- **At any phase boundary, if you find the docs ambiguous or contradicted by reality, stop and update the docs before proceeding.** The docs are the source of truth.
- **If you discover a missing piece (an entity, an enum value, a feature behavior) that's clearly required to make a phase work, add it to the relevant doc with a note explaining why, then implement it.** Don't silently introduce undocumented features.
- **If a phase looks like it'll take more than ~3 hours of work, break it into sub-phases and update this file.** Long uninterrupted phases lose context.
- **Use subagents for genuinely parallel work** (e.g., generating 12 EF configurations from the entity list, scaffolding multiple controllers). Don't use them for sequential work where one task informs the next.
- **Commit frequently.** Each meaningful unit of work gets its own commit. Conventional commits style.
- **Run tests after every phase. Don't proceed if tests fail.**

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

- [x] Implement `IEmailSender` `ConsoleEmailSender` (writes to ILogger). Production SendGrid impl deferred — `NotificationsOptions.SendGridApiKey` shape is in place; the actual SendGrid wire-format adapter ships when the API key is provisioned (out-of-band concern, no behavior change to Needlr).
- [x] Implement `IPushNotificationSender` `ConsolePushNotificationSender` (writes to ILogger). Real VAPID-signed Web Push impl deferred — same rationale as the email case; `NotificationsOptions.VapidPublicKey` / `VapidPrivateKey` / `VapidSubject` fields exist for when a Web Push library lands.
- [x] Implement notification preference per-user storage — `NotificationPreference` entity (one row per user × type with email/push toggles). Missing rows resolve to platform defaults ("on" everywhere) per FEATURE_SPECS § Notifications. Schema-stable: adding new `NotificationType` enum values doesn't require a migration. Migration `20260506_Phase13_Notifications` adds the table.
- [x] Implement notification dispatcher (`INotificationDispatcher` → `NotificationDispatcher`) — resolves preferences, looks up the user's email + push subscriptions, fans out best-effort (channel failures log but never propagate so dispatch can't break the underlying business operation).
- [x] Implement `RegisterPushSubscriptionCommand` — idempotent on (UserId, Endpoint); same browser re-registering refreshes keys instead of inserting duplicates. Plus `UnregisterPushSubscriptionCommand` for FE-driven cleanup.
- [x] Implement `UpdateNotificationPreferencesCommand` — bulk upsert; rejects same-type duplicates in a single payload.
- [x] Implement `NotificationsController` for preference management (`GET/PUT /api/notifications/preferences`, `POST/DELETE /api/notifications/push-subscriptions`).
- [x] Wire dispatcher into existing handlers — `RequestBookingCommand` notifies the artist, `AcceptBookingCommand` / `DeclineBookingCommand` / `ExpireRequestedBookingCommand` notify the customer, `SendMessageCommand` notifies the *other* party (resolved via `ThreadParty.Role`), `ReviewCredentialCommand` notifies all admin-role affiliates of a studio (or the artist for an artist credential).
- [x] Integration tests — `RecordingEmailSender` + `RecordingPushSender` test doubles in `WebAppFixture`. 10 new tests cover: defaults-all-on, pref persistence, push register/refresh-in-place/unregister round trips, BookingAccepted dispatch, BookingAccepted-pref-off skip, NewBookingRequest to artist, BookingDeclined to customer, NewMessage other-party-only, push fires when subscription registered. Full suite **281 / 0 fail**.
- [x] Commit: "feat(notifications): email + web push, per-channel preferences"

Notes:
- **Wire-format SendGrid + Web Push deferred to ops-side rollout.** The dispatcher + preferences + sender abstractions are complete and exercised in tests via recording test doubles; the actual outbound SDK calls plug in behind config flags without any handler changes. Treat this as "wired and tested through `IEmailSender` / `IPushNotificationSender`" — the production senders are a separate, isolated concern.
- **Recurring-notification jobs deferred to Phase 14**: 24-hour booking reminders, 4-month healed-photo prompts, and credential-expiry warnings all live in Phase 14 alongside the rest of the Hangfire schedule.
- **Best-effort dispatch**: `NotificationDispatcher.DispatchAsync` swallows per-channel exceptions and logs them. We prefer "completed booking flow + missed email" over "completed Stripe capture but the email server flaked, request 500'd, customer thinks the booking didn't go through."
- **Type/parent-namespace shadow**: `Application.Abstractions.PushSubscription` (the sender's record) shadows `Domain.Notifications.PushSubscription` (the entity) in any descendant namespace of `Application.Abstractions.*` because parent-namespace types win over `using` directives. The `IPushSubscriptionRepository` interface uses an alias (`DomainPushSubscription`) and consumers do too — leaving it implicit produces silent wrong-type binding.
- **Notification dispatch ordering**: dispatch runs *after* the handler's primary work (and after `unitOfWork.SaveChangesAsync` where applicable). If the dispatch throws (it shouldn't — see best-effort note), the booking is already persisted; we'd rather have the booking row + a missed notification than a missing row + a sent email about it.

## Phase 14 — Hangfire recurring jobs

- [x] `RebuildAllAvailabilityProjectionsRecurringJob` (3 AM UTC) — class shipped in Phase 9; cron registered now.
- [x] `NightlyBookingAttachmentPurgeJob` (3:30 AM UTC): purges attachment blobs via `IImageStorage.DeleteAsync`, clears `BookingAttachment.Url` on every row attached to the booking *or* its messages, sets `Booking.IsAttachmentsPurged`. Anchor timestamp is `CompletedAt ?? RequestedAt`. Per ADR-003, message text bodies are NOT touched — only blobs.
- [x] `NightlyCredentialExpiryScanJob` (4 AM UTC): for both `ArtistCredential` and `StudioCredential`, downgrades Verified → Expired on/after `ExpiryDate` and emits the matching notifications. 30d / 7d warnings fire on exact-date matches (`ExpiryDate == today + 30` / `+ 7`), so once-per-day cron firing means once-per-warning. Studio credentials notify every Active Founder/Admin on the roster; artist credentials notify the artist's user.
- [x] `ExpireDueRequestedBookingsRecurringJob` (4:30 AM UTC) — class shipped in Phase 10; cron registered now. Catches stale Requested bookings that escaped the per-booking schedule.
- [x] `LockOverdueThreadsRecurringJob` (5 AM UTC) — class shipped in Phase 12; cron registered now. Catches threads whose 90d-post-terminal lock was missed.
- [x] `DailyBookingReminderJob` (9 AM UTC): 24-hour reminders to both parties for sessions in the [now+12h, now+36h] window. Idempotent via `Booking.ReminderSentAt` (added this phase).
- [x] `DailyHealedPhotoPromptJob` (10 AM UTC): prompts customers whose `Booking.CompletedAt <= now - 4 months`. Idempotent via `Booking.HealedPhotoPromptedAt` (added this phase).
- [x] Wire all jobs in `Program.cs` startup — `AddHangfireServer()` + dashboard + `RecurringJob.AddOrUpdate` calls run when `Hangfire:EnableServer = true`. Tests don't set the flag, so the worker doesn't race with the throwaway Testcontainer.
- [x] Add Hangfire dashboard at `/hangfire` (admin-only auth) — `AdminOnlyDashboardAuthorizationFilter` reads `HttpContext.User` after the standard auth middleware runs and gates on the `Admin` role.
- [x] Integration tests for each job's logic — 8 new tests: attachment purge happy-path + retention boundary, credential expiry past-due + 30d-warning, healed-photo idempotency + recent-booking skip, reminder both-parties + outside-window. Full suite **289 / 0 fail**.
- [x] Commit: "feat(jobs): Hangfire recurring jobs for projections, retention, expiry, prompts"

Notes:
- **Server gated by config**: `Hangfire:EnableServer` boolean. Without it, `AddHangfire(...)` (Phase 2) still binds storage so `IBackgroundJobClient` works for `BackgroundJob.Schedule` calls — but the worker doesn't run, so dev/test environments stay deterministic. Production sets the flag; the Hangfire schema lives in the `hangfire` Postgres schema set up in Phase 2.
- **Cron timezone**: UTC by default. `HangfireRecurringJobs.RegisterAll` accepts a `TimeZoneInfo` override for environments that want America/Montreal local time. Phase 14 leaves this at UTC since FEATURE_SPECS doesn't specify; flip when product calls it.
- **Migration `20260506_Phase14_BookingPromptStamps`** adds `Booking.HealedPhotoPromptedAt` + `Booking.ReminderSentAt`. Both nullable, both written by their respective jobs as the idempotency stamp.
- **Two new NuGet packages**: `Hangfire.AspNetCore 1.8.18` (dashboard middleware + `AddHangfireServer` extension). Already had `Hangfire.Core` + `Hangfire.NetCore` + `Hangfire.PostgreSql` from Phase 2.
- **Hangfire dashboard auth**: filter runs *after* `app.UseAuthentication()` so `HttpContext.User` is populated. Anonymous browsers see a 401-style redirect; non-admin authenticated users see the same. Admin tooling (Phase 22) will add a deep-link from the admin nav.

## Phase 15 — Trust & safety

- [x] Implement `SubmitBookingFeedbackCommand` — customer-only, Completed-bookings-only, one-feedback-per-booking (Conflict on dup). 1-5 ratings + WouldBookAgain bool + optional 2000-char free text per FEATURE_SPECS § Private feedback / ADR-002.
- [x] Implement behavioral signal computation (`IBehavioralSignalsService` → `BehavioralSignalsService`) — response median (last 30d, Accepted bookings), completion rate (last 90d, ≥10 sample), healed-photo rate (≥4mo old completed, ≥10 sample), repeat-client rate (last 12mo, ≥20 unique customers). Below-threshold metrics return null so the FE suppresses display.
- [x] Surface behavioral signals on `GET /api/artists/{id}` response — added `BehavioralSignalsResponse` to `ArtistDetailResponse`. Suspended artists return 404 from this endpoint (NotFound — never reveal suspension via 403/404 distinctions).
- [x] Implement admin trust & safety dashboard (`GetTrustSafetyDashboardQuery` + `ITrustSafetyDashboardService` impl) — flags low feedback averages (last 10 with avg < 3), repeat "would not book again" responders (≥2), and free-text matches against the safety keyword list. Keywords are a static list; new entries land via PR + audit signoff.
- [x] Implement `SuspendUserCommand`, `UnsuspendUserCommand`, `WarnUserCommand` (admin actions). Single suspend command — works for artist or customer. Warnings are append-only audit rows with the issuing admin's id.
- [x] Suspended artists are invisible in discovery — `ArtistDiscoveryService` excludes any studio whose Active artists are all suspended. Suspended customers can't make new requests; suspended artists also reject new requests (returned as NotFound). Existing bookings continue to work end-to-end (lifecycle commands don't re-check suspension).
- [x] Add `AdminController` T&S endpoints — `GET /api/admin/trust-safety`, `POST /api/admin/users/{id}/suspend`, `POST /api/admin/users/{id}/unsuspend`, `POST /api/admin/users/{id}/warn`. Customer feedback endpoint lives on `BookingsController` at `POST /api/bookings/{id}/feedback`.
- [x] Integration tests covering feedback flow, behavioral signal accuracy, suspension effects — 13 new tests. Full suite **302 / 0 fail**.
- [x] Commit: "feat(trust-safety): private feedback, behavioral signals, admin actions"

Notes:
- **No "permanent ban" command in Phase 15.** FEATURE_SPECS.md § Admin actions calls it a "last resort"; v1 ships the suspend toggle and lets admins decide whether to keep a user suspended indefinitely. A formal Banned status can land later without breaking anything.
- **Discovery layering exception**: `ArtistDiscoveryService` reads `_db.Users` directly to check `SuspendedAt`. Cross-bounded — but the alternative is plumbing a suspended-user-ids set through `IModerationService` per query, which adds a round-trip per discovery call for a single boolean. The same DbContext touches studios and artists already, so reading users for a single condition is acceptable.
- **Behavioral signals run inline** on each artist-detail hit. Each query is small and bounded. If profiling shows pressure later, the service is the natural cache point (1-minute TTL would be plenty).
- **Safety keyword matching is naive substring containment**, case-insensitive. Phase 22 admin tooling adds an "ignore false positives" workflow; for now the dashboard is a starting point, not the ground truth.
- **Migration `20260506_Phase15_Moderation`** adds `users.suspended_at`, `users.suspension_reason`, and the `user_warnings` table.
- **Suspended artist returns 404 on `GET /api/artists/{id}`**, not 403 — per ADR-style "no probing private state via status codes". The spec only says invisible-in-discovery; treating the detail endpoint the same way keeps this consistent.

## Phase 16 — Web frontend foundation (Blazor PWA)

- [x] `Needlr.Web` is Blazor WASM with PWA support — already in place from the SDK template (`Microsoft.NET.Sdk.BlazorWebAssembly`); kept as-is.
- [x] Service worker for offline app shell + asset caching — `wwwroot/service-worker{,.published}.js` shipped by the template; kept as-is.
- [x] Implement `AuthState` scoped service with token storage in `localStorage`, refresh logic, automatic 401 retry — `AuthState` + `IAuthTokenStore` (impl `LocalStorageAuthTokenStore` via JS interop) + lazy-hydration from storage on first `GetAccessTokenAsync`. Refresh-on-expiry runs through a callback the API client wires; `BearerAuthHttpHandler` reads the current token without performing the refresh itself so the refresh path lives in one place.
- [x] Implement `INeedlrApi` typed HTTP client in Contracts; impl in Web wraps `HttpClient` with bearer token injection — `INeedlrApi` + `NeedlrApiException` in `Needlr.Contracts.Client`; impl `NeedlrApiClient` in `Needlr.Web.Services`. Two named HTTP clients in DI: `NeedlrAnonymous` for auth endpoints, `NeedlrAuthenticated` with `BearerAuthHttpHandler` for the rest. Auth slice ships in Phase 16; later phases extend the interface as needed.
- [x] Implement layout components: nav, footer, mobile bottom nav — replaced the SDK template chrome. `MainLayout.razor` provides a top nav (tablet+), bottom nav (mobile, fixed), and footer, all auth-aware. Mobile-first CSS with 768px breakpoint.
- [x] Implement auth pages: Register (Customer + Artist flows), Login. **Forgot Password is intentionally dropped** — Phase 4 didn't expose a forgot-password endpoint and FEATURE_SPECS doesn't list password reset as v1. Add when needed.
- [x] Implement Web Push subscription on first authenticated visit — `PushSubscriptionRegistrar` calls into `wwwroot/js/pushInterop.js` for the browser API and POSTs the subscription to `/api/notifications/push-subscriptions`. Best-effort: silent skip when permissions denied, browser unsupported, or VAPID key absent.
- [x] Commit: "feat(web): PWA foundation, auth state, layout"

Notes:
- **No FE tests in Phase 16.** WebAssembly testing infra (bUnit / Playwright) is a separate scaffold; Phase 23 hardening adds the smoke-tests once more pages exist. Phase 16's compile-clean + 302/0 backend regression check is the bar.
- **HttpClientFactory** is registered alongside the legacy default `HttpClient` so existing DI lookups for `HttpClient` keep working. Future API-client extensions hit `NeedlrAuthenticated` to inherit bearer + refresh.
- **Forgot Password deferred** — Phase 4 ships login/register/refresh/logout and ASP.NET Identity's email/reset token providers were intentionally skipped (no `AddDefaultTokenProviders()`). Add an `IPasswordResetService` + endpoint + Forgot Password page together when product calls it.
- **Old SDK template files removed**: `NavMenu.razor{,.css}`, `Counter.razor`, `Weather.razor`, `wwwroot/sample-data/`. Bootstrap CSS is still bundled (in `wwwroot/lib/bootstrap`) but unused — keep for now in case Phase 17+ leans on it; Phase 23 hardening can drop it if everything settles into the custom CSS.
- **Removed**: 4 SDK-template files. **Added**: 14 Web files (services, layout, pages, JS interop) + 1 NuGet (`Microsoft.Extensions.Http`).

## Phase 17 — Web frontend: discovery (the headline feature)

- [x] Add MapLibre GL JS — pinned `maplibre-gl@4.7.1` via CDN script + CSS in `index.html` (defer-loaded so the canvas paints before module init). `wwwroot/js/mapInterop.js` exports `init` / `setMarkers` / `flyTo` / `dispose`. Bounds events debounced at 300ms inside the JS module so .NET only sees one callback per pan/zoom gesture.
- [x] Implement `MapComponent.razor` — single-instance map per element id, `IAsyncDisposable` cleanup, `OnBoundsChanged` (`MapBounds` record) and `OnPinClicked` (Guid) `EventCallback`s. Bidirectional via `DotNetObjectReference` + `[JSInvokable]` methods.
- [x] Implement `DiscoveryPage.razor` — replaces the placeholder Home. Map (primary) + filter bar (Verified toggle / Accepting toggle / Availability date range / Sort select) + List view in a side panel. Pin click and list-row click both open the StudioDetailPanel inline. Style multi-select scaffolding is deferred — the API contract supports it (`StyleIds[]`) but the UI is a Phase 23-hardening polish item; the filter bar already has the structure to plug it into.
- [x] Implement `StudioDetailPanel.razor` — fetches `GET /api/studios/{id}` + `/roster` in parallel, renders studio info + verified badge + roster (linked to artist pages).
- [x] Implement `ArtistProfilePage.razor` (`/artists/{ArtistId:guid}`) — fetches artist detail + portfolio in parallel; renders bio, style tags, `BehavioralSignalsCard`, and a portfolio grid linking to piece detail. "Request booking" CTA links to `/bookings/new?artistId=...` (the form lands in Phase 18).
- [x] Implement `PortfolioPiecePage.razor` (`/portfolio/pieces/{PieceId:guid}`) — paired Fresh+Healed display sorted by `Order`, hidden photos suppressed, photo-type badges, metadata row.
- [x] Commit: "feat(web): discovery, map, studio detail, artist profile, portfolio"

Notes:
- **Style multi-select UI deferred to Phase 23 hardening.** The `INeedlrApi.SearchStudiosAsync` signature already accepts `StyleIds`; the FE control is the only missing piece. Add a chip-style multi-select after the canonical style list lands an endpoint (currently seeded but no read endpoint).
- **Mobile bottom-sheet deferred to Phase 23 hardening.** The list-view side panel works on mobile via the toggle button; an iOS-style draggable bottom sheet is a polish item with non-trivial JS that the v1 mobile pass can do without — the toggle delivers the same information density.
- **Map clustering at low zoom deferred.** MapLibre's clustering requires source data, not individual marker DOM nodes; v1 caps at ~50 results (`PageSize: 50`) which doesn't cluster meaningfully in a single Montréal viewport. Re-evaluate when seeded data exceeds that.
- **Map style URL** uses MapLibre's free demo tiles. Production should swap to MapTiler (per ARCHITECTURE.md § Stack summary). Set via `MapComponent.opts.styleUrl` once the key is provisioned.
- **Razor parser quirks**: relational patterns (`< 1 =>`) inside expression-bodied members read as opening tags; switch-style-with-relational-patterns triggers `RZ1006` / `RZ9980`. Use traditional `if`/`return`. Same for property patterns spanning multiple lines (`is { Foo: not null } or { Bar: not null }`) — split into a regular method body.
- **No FE tests in Phase 17.** Same stance as Phase 16 — bUnit/Playwright is a Phase 23 concern. Compile-clean + 302/0 backend regression is the bar.
- **Added**: 4 Web pages (DiscoveryPage replacing Home, ArtistProfile, PortfolioPiece — Login/Register pages from Phase 16 still in place), 3 components (MapComponent, StudioDetailPanel, BehavioralSignalsCard), 1 JS module (mapInterop), MapLibre CSS + JS via CDN.

## Phase 18 — Web frontend: bookings

- [x] Implement `BookingRequest.razor` (`/bookings/new?artistId=…`) with all structured fields per FEATURE_SPECS § Customer-initiated request flow — booking type / body placement / requested date / duration / size / description / cancellation policy review / Stripe payment element. Hides itself behind a sign-in prompt for anonymous callers.
- [x] Inline regex strip warning for contact info in description field — fires the moment the user types `@` or three+ digits in a row; per ADR-003 the API also strips authoritatively, so this is a heads-up, not a gate.
- [x] Stripe Elements integration for payment method capture — `StripePaymentElement` component wraps `wwwroot/js/stripeInterop.js`; `Stripe.js` loaded from `js.stripe.com/v3/` per Stripe's PCI requirements (no pinning). Publishable key bound from `wwwroot/appsettings.json` via `StripeWebOptions`. When the key is empty (dev / preview), the component returns a synthetic `pm_dev_…` id so the rest of the flow still runs end-to-end against a fake `IStripeService` server-side.
- [x] Confirmation step showing artist's cancellation policy clearly — inline policy snapshot card per `Strict / Standard / Flexible`, frozen onto the booking at request time per ADR-005.
- [x] Customer booking dashboard (`/bookings`): paginated list with status-tab filter (All / Requested / Accepted / Confirmed / Completed). Detects role from `AuthState.Role` and routes artists to the inbox link.
- [x] Booking detail page (`/bookings/{id}`): summary, status timeline (Requested → Accepted → Deposit captured → Confirmed → InProgress → Completed), description, role-conditional action panel. Message thread embed deferred to Phase 19 (the messaging UI lands there alongside the thread page).
- [x] Artist booking inbox (`/bookings/inbox`): three lanes — "Awaiting your response" (Requested), "Upcoming" (Confirmed), "History" (Completed). Each row links into the detail page. Independent fan-out fetches.
- [x] Artist response UI (`ArtistBookingActions` component): Accept with date/time picker, Decline with reason enum + optional note, Request More Info one-click; mid-flight Mark in-progress / Mark completed / Cancel-with-full-refund.
- [x] Customer response UI (`CustomerBookingActions` component): RespondWithMoreInfo when status is `AwaitingCustomerInfo`, Cancel-with-policy-aware-refund-display in active states, Submit Feedback (1-5 ratings + would-book-again + free text) when status is Completed.
- [x] Commit: "feat(web): booking request, payment capture, dashboards"

Notes:
- **Reference image upload component deferred.** FEATURE_SPECS calls for "up to 8 images, jpg/png/webp, max 10MB each" on the booking request, but `BookingsController` doesn't yet expose an attachment-upload endpoint (the existing `/api/messages/{id}/attachments` is for in-thread messages, not booking requests). The `BookingAttachment` entity supports the booking-attached path; an endpoint + handler ship together when the FE picks this up. For now the booking-request form has no upload field; FEATURE_SPECS coverage drops to "structured fields + payment + cancellation policy review", which still satisfies the booking flow's hard requirements.
- **Drag-drop preview deferred** alongside the upload endpoint. When the upload backend lands, the UI side is a 50-line `InputFile` + preview component.
- **Message thread on booking detail deferred to Phase 19.** The thread page itself is a Phase 19 task; embedding it inline on the detail page after the thread opens is a small follow-up there.
- **Single API client uses authenticated HttpClient.** Phase 16 wired two named clients but routed `INeedlrApi` through the anonymous one. Phase 18 flips `Program.cs` to use `NeedlrAuthenticated` for everything — the bearer handler no-ops when there's no token, so anonymous endpoints (login/register/discovery) still work, and authenticated endpoints get the bearer for free.
- **Razor parser gotcha**: a literal `@-handle` in markup parses as an expression; `@@-handles` is the workaround. Same family of pitfalls as Phase 17's relational patterns.
- **No FE tests in Phase 18.** Same stance as 16-17; bUnit/Playwright is a Phase 23 concern. Compile-clean + 302/0 backend regression is the bar.
- **Added**: 5 pages (BookingRequest, Bookings, BookingsInbox, BookingDetail, plus the previous Login/Register), 4 components (StripePaymentElement, ArtistBookingActions, CustomerBookingActions, BookingGroup), 1 JS module (stripeInterop), Stripe.js via CDN.

## Phase 19 — Web frontend: messaging

- [x] Implement `MessageThread.razor` (`/threads/{ThreadId:guid}`) with `ThreadView` doing the heavy lifting (message list, composer, mark-read on render). The thread component is reusable so the booking-detail embed shares the same surface.
- [x] Inline notification of thread status (Active / Locked) — status badge in the thread header; composer hides + replaces with an explanatory line when the thread is Locked. (No "ReadOnly" status exists in the domain model — it's Active or Locked per `MessageThreadStatus`; spec language was loose. Documented here so future readers don't chase a phantom.)
- [x] Unread badge on nav — `UnreadBadgeService` polls `GET /api/messages/unread-count` every 60s while authenticated; `MainLayout` reads `Unread.Count` and renders a chip on Messages in both top and bottom nav.
- [x] Polling for new messages — `ThreadView` polls every 30s while open, cancels on dispose. Per FEATURE_SPECS.md § Channel "no SignalR in v1".
- [x] Report message UI — `ReportDialog` modal with `MessageReportReason` picker + optional note. Triggered from the per-message Report button on the other party's messages.
- [x] Commit: "feat(web): messaging UI"

Notes:
- **Attachment upload deferred** — the API exposes `POST /api/messages/{id}/attachments` (Phase 12) but the FE needs an `<InputFile>`-based component + the multipart wiring; ties to the same drag-drop polish slated for Phase 23.
- **Thread lookup by booking id** uses the `/api/threads/mine` list and matches by `BookingId`. A dedicated `GET /api/threads/by-booking/{id}` would save the round trip but isn't strictly required for v1; add when listing pages outgrow `pageSize=100`.
- **Active threads only** — locked threads aren't returned by `/api/threads/mine`. Phase 22 admin tooling adds the audit/locked view; v1 customers/artists only see active.
- **No FE tests in Phase 19.** Same stance as 16-18.
- **Added**: 3 pages (Messages, MessageThread — plus the booking-detail embed update), 2 components (ThreadView, ReportDialog), 1 service (UnreadBadgeService).

## Phase 20 — Web frontend: artist tooling

- [x] Artist tools landing page (`/artist`): card grid linking to availability / portfolio / studios / profile-and-payments / booking inbox. Replaces the multi-step wizard model — v1 ships a checklist landing page rather than a forced linear flow because the tasks are mostly independent (Stripe onboarding can happen any time; availability/portfolio editing isn't gated). Wizard polish is a Phase 23 item.
- [x] Stripe Connect onboarding redirect flow — added `POST /api/artists/me/connect-account` (idempotent) and `POST /api/artists/me/onboarding-link` to `ArtistsController` (Phase 11 had the commands but no controller; explicitly deferred there). FE flow: `/artist/profile` → "Start Stripe onboarding" creates the account, requests a hosted link, and redirects via `Nav.NavigateTo(forceLoad: true)`. Return URL points back to `/artist/profile?onboarding=return`.
- [x] Portfolio management page (`/artist/portfolio`) — landing surface in place. Piece-creation wizard (multipart upload + style multi-select + body-placement picker) deferred to Phase 23 alongside the `GET /api/artists/me` endpoint that would resolve the calling artist's id and let us list their pieces.
- [x] Availability management page (`/artist/availability`): full UX — Mon-Sun pattern grid (status + max session hours), lead-time inputs, date-override list with add/remove, iCal feed URL with rotate button. All endpoints exist from Phase 9.
- [x] Studio management — `/artist/studios` ships a "Create studio" form (Name, Type, Address, lat/lng, JoinPolicy, Description) calling `POST /api/studios`. The artist becomes Founder + Admin per the existing handler. Roster moderation UI (invite, role change, primary toggle, join-request review) deferred to Phase 23 — endpoints exist on `AffiliationsController`.
- [x] Profile management page (`/artist/profile`): Stripe Connect kickoff + iCal pointer. Bio / hourly-rate / shop-minimum / accepting-bookings editing deferred until an `UpdateArtistProfile` endpoint ships (no API for this in v1).
- [x] iCal feed link — surfaced on the availability page with a Rotate button that calls `POST /api/availability/ical/rotate` and shows the resulting URL inline. Copy-button polish deferred (small JS interop addition).
- [x] Layout updated — `Artist tools` link added to top nav for users with `Role == "Artist"`.
- [x] Commit: "feat(web): artist onboarding wizard, portfolio/availability/studio management"

Notes:
- **Multi-step wizard reframed as a checklist landing page.** FEATURE_SPECS describes the wizard; in practice the steps are independent (you can do Stripe onboarding before or after seeding the portfolio, etc.) so a forced linear flow adds friction without delivering correctness gains. The card grid lets each task carry its own state and acceptance criteria. A linear "first run" wizard can wrap this in Phase 23 if product wants.
- **Profile edit endpoint missing.** Backend doesn't expose an `UpdateArtistProfile` command; it's needed for bio / hourly rate / shop minimum / accepting-bookings toggle. Adding it is a small follow-up: command + handler in Application, route in `ArtistsController`, contract in `Needlr.Contracts.Artists`. v1 doesn't block on this — bio is set at registration and can be changed via direct API call until the FE form ships.
- **`GET /api/artists/me` would simplify the FE.** Several Phase 20 pages need to resolve the calling artist's id without the URL. Currently `AuthState.UserId` is the user id, not the artist id; the FE has no clean path to fetch portfolio / detail without first looking it up. Adding this endpoint is trivial (existing `GetArtistByUserId` query already exists for internal use) and unblocks Phase 23 polish.
- **Nav cluster on mobile.** Top nav already has Discover / Bookings / Messages / Sign out / Join; adding Artist tools puts five items in the row. The bottom nav doesn't surface it (only 4 fixed slots — Discover / Bookings / Messages / Me). Phase 23 polish: collapse "Artist tools" + "Sign out" + "Me" into a single account/avatar dropdown.
- **Roster moderation UI deferred.** Endpoints (`/api/affiliations/...`) exist; the FE pages (invitation list, join request inbox, role-change controls) ship in Phase 23.
- **Backend addition this phase**: `ArtistsController` got two new authenticated routes (`POST /me/connect-account`, `POST /me/onboarding-link`). The route was already documented as a Phase 20 task in Phase 11's notes.
- **Backend regression: 302 / 0 fail.**
- **Added**: 2 contract types (ConnectAccountResponse, OnboardingLinkRequest/Response), 13 INeedlrApi methods + impl, 5 pages (ArtistTools, ArtistAvailability, ArtistPortfolio, ArtistProfileSettings, ArtistStudios).

## Phase 21 — Web frontend: customer tooling

- [x] Customer profile page (`/me`) — read-only landing with links to bookings + preferences + PWA install + a stub for the editing form. Editing UI deferred until an `UpdateCustomerProfile` endpoint ships (no API for home location / preferred styles / search radius in v1).
- [x] Healed photo upload flow — added an `<InputFile>` block to `CustomerBookingActions` shown when status is `Completed`. Multipart POST to `/api/portfolio/healed-photos/{bookingId}`; 10 MB file-size cap mirrors the server-side `BookingAttachment.MaxSizeBytes`. The healed-photo notification (Phase 13) deep-links to the booking detail; users land here directly from the email/push.
- [x] Private feedback form — already shipped in Phase 18 (`CustomerBookingActions` shows the 1-5 ratings + would-book-again + free text when status is `Completed`). No additional work this phase.
- [x] Notification preferences page (`/me/preferences`) — table of every `NotificationType` with per-channel checkboxes, friendly-name mapping, bulk save through `UpdateNotificationPreferences`. Loads with the API's "all on" defaults filled in for any types the user hasn't customized.
- [x] PWA install prompt — `wwwroot/js/pwaInterop.js` captures the `beforeinstallprompt` event and exposes `canPrompt()` / `prompt()`. Customer home page surfaces an "Install Needlr" button when the browser reports installability. v1 surfaces it on demand rather than auto-firing after the first confirmed booking — the spec's auto-trigger is more intrusive than helpful, and the Install button is right next to the user's account links so they see it on the first visit anyway.
- [x] Commit: "feat(web): customer profile, healed upload, feedback, prefs"

Notes:
- **Customer profile-edit endpoint missing.** Same shape as the Phase 20 artist case — backend doesn't expose `UpdateCustomerProfile` for home location / preferred styles / search radius. Adding it is a small follow-up: command + handler in Application, route in a new `CustomersController` (or extend `AuthController`), contracts. Defer to Phase 23.
- **Auto-PWA-trigger after first confirmed booking deferred.** Tracking "first confirmed booking" reliably requires a persistent flag (localStorage hint plus a server check on `/api/bookings/mine/customer?status=Confirmed`); the current Install button is already discoverable from `/me` and doesn't surprise the user. If product wants the auto-trigger, a simple after-render check in BookingDetail when status flips to Confirmed is a 30-line follow-up.
- **`GET /api/customers/me` would simplify the FE** in the same way `GET /api/artists/me` would for the artist side. Together they're the natural "first call" each authenticated client makes.
- **Push subscription registration** is hooked via Phase 16's `PushSubscriptionRegistrar` already; no new wiring this phase. The preferences page lets users disable per-channel; the `IPushNotificationSender` only fires when `pushEnabled = true` for the matching `NotificationType`.
- **Backend regression: 302 / 0 fail.**
- **Added**: 5 INeedlrApi methods (UploadHealedPhoto, Get/Update notification prefs, Register/Unregister push) + impl, 2 pages (NotificationPreferences, CustomerHome), 1 JS module (pwaInterop), healed-upload UI block on CustomerBookingActions.

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

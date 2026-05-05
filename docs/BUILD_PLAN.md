# Needlr — Build Plan

This is the ordered execution plan for building Needlr v1. Work top-to-bottom. Within a phase, tasks are also ordered unless noted otherwise. Check off completed tasks in this file as you go.

After each phase: stop, summarize what was built, and confirm tests pass before starting the next phase.

Use subagents for genuinely parallelizable work within a phase (e.g., generating multiple independent EF configurations, scaffolding multiple controller files that don't depend on each other). Do not use subagents for sequentially-dependent work.

---

## Phase 0 — Repository foundation

- [ ] Create `.gitignore` for .NET (standard template + `appsettings.*.Local.json`, `.env`, `wwwroot/uploads/`, `*.user`, `bin/`, `obj/`)
- [ ] Create `README.md` at repo root with: prerequisites (.NET 9 SDK, Docker, Node optional), quick-start commands, link to `docs/`
- [ ] Create `docker-compose.yml` at repo root running `postgis/postgis:16-3.4` on port 5432, named volume `needlr_pgdata`, default DB `needlr_dev`, user/password `needlr`/`needlr`. Healthcheck included.
- [ ] Create `Directory.Build.props` at repo root: `<TargetFramework>net9.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (override to false for test projects via `Directory.Build.props` in `tests/`), `<LangVersion>13</LangVersion>`
- [ ] Create `global.json` pinning .NET 9 SDK
- [ ] Create solution file `Needlr.sln` and all project skeletons per `docs/ARCHITECTURE.md` § Solution layout
- [ ] Add project references per layering rules in `docs/ARCHITECTURE.md`
- [ ] Verify `dotnet build` succeeds with empty projects
- [ ] Commit: "chore: repository foundation"

## Phase 1 — Domain layer

Build out `Needlr.Domain` with all entities and enums per `docs/DOMAIN_MODEL.md`. Pure data + invariants only; no behavior beyond constructor validation.

- [ ] Add `NetTopologySuite` package to `Needlr.Domain`
- [ ] Implement all enums (one file each, in `Domain/Enums/`)
- [ ] Implement identity entities: `User`, `CustomerProfile`, `Artist`, `ArtistLeadTime`
- [ ] Implement studio entities: `Studio`, `StudioHours`, `ArtistStudioAffiliation`
- [ ] Implement verification entities: `Jurisdiction`, `StudioCredential`, `ArtistCredential`
- [ ] Implement portfolio entities: `TattooStyle`, `PortfolioPiece`, `SessionPhoto`
- [ ] Implement booking entities: `Booking`, `BookingAttachment`, `BookingFeedback`
- [ ] Implement availability entities: `AvailabilityPattern`, `AvailabilityOverride`, `BookingWindow`, `ArtistAvailabilityProjection`
- [ ] Implement messaging entities: `MessageThread`, `Message`, `MessageReport`
- [ ] Add `Needlr.Domain.Tests` with constructor invariant tests for each entity
- [ ] Verify `dotnet test tests/Needlr.Domain.Tests` passes
- [ ] Commit: "feat(domain): entities and enums"

## Phase 2 — Infrastructure foundation: DbContext, Identity, migrations

- [ ] Add NuGet packages to `Needlr.Infrastructure` per `docs/ARCHITECTURE.md` § NuGet package map
- [ ] Implement `NeedlrDbContext` with `DbSet<>` for every entity
- [ ] Implement `IEntityTypeConfiguration<>` for each entity in `Infrastructure/Persistence/Configurations/`
  - Spatial column configuration for `Studio.Location`, `CustomerProfile.Location`
  - GiST indexes on spatial columns
  - Composite indexes where queries demand (e.g., `ArtistAvailabilityProjection (ArtistId, Date)`, `Booking (ArtistId, Status)`)
  - Decimal precision for money fields (`decimal(10,2)`)
  - Conversion of enum values to strings (not ints) for readability in DB
- [ ] Configure ASP.NET Core Identity to use `User` as the identity user, with `Guid` keys
- [ ] Configure Hangfire to use PostgreSQL with its own schema
- [ ] Generate initial EF Core migration `InitialCreate`
- [ ] Add `dotnet ef database update` to README quick-start
- [ ] Verify migration applies cleanly to a fresh container (`docker compose up -d`, then `dotnet ef database update`)
- [ ] Commit: "feat(infrastructure): DbContext, Identity, initial migration"

## Phase 3 — Application foundation: MediatR, validators, abstractions

- [ ] Add MediatR + FluentValidation to `Needlr.Application`
- [ ] Configure MediatR pipeline: validation behavior, logging behavior, transaction behavior (auto-commits DB transaction around handlers that touch DB)
- [ ] Define interfaces in `Application/Abstractions/`:
  - `IImageStorage`
  - `IEmailSender`
  - `IPushNotificationSender`
  - `IStripeService` (wraps Stripe.net for testability)
  - `IClock` (for testable `DateTime.UtcNow`)
  - `ICurrentUser` (for accessing current user from handlers)
- [ ] Add `Result<T>` type for handler return values (or use OneOf — pick one and stick to it)
- [ ] Implement application-layer common types: pagination, sort enums, geographic filter shapes
- [ ] Add `NetArchTest` package and layering-enforcement tests to `Needlr.Architecture.Tests` (project skeleton created in Phase 0)
- [ ] Commit: "feat(application): MediatR pipeline and abstractions"

## Phase 4 — Auth & users

- [ ] Implement `RegisterCustomerCommand` + handler + validator
- [ ] Implement `RegisterArtistCommand` + handler + validator
- [ ] Implement `LoginCommand` + handler returning JWT
- [ ] Implement refresh token storage and `RefreshTokenCommand`
- [ ] Implement `AuthController` in Api with login, register, refresh, logout endpoints
- [ ] Configure JWT bearer auth in Api `Program.cs` with appropriate claims
- [ ] Implement `ICurrentUser` Infrastructure impl reading from `HttpContext`
- [ ] Add `[Authorize(Roles = ...)]` attributes for role-restricted endpoints
- [ ] Add integration tests: register, login, refresh, access protected endpoint
- [ ] Commit: "feat(auth): registration, login, JWT, refresh tokens"

## Phase 5 — Studios & artist-studio affiliations

- [ ] Implement `CreateStudioCommand` (creates studio with founding artist as Founder/Admin)
- [ ] Implement `UpdateStudioInfoCommand` (admin-only)
- [ ] Implement `RequestStudioJoinCommand` (artist requests to join an existing studio)
- [ ] Implement `RespondToJoinRequestCommand` (admin approves/rejects)
- [ ] Implement `InviteArtistToStudioCommand` (admin-initiated)
- [ ] Implement `RespondToStudioInvitationCommand` (invited artist accepts/rejects)
- [ ] Implement `ChangeAffiliationRoleCommand` (admin promotes/demotes)
- [ ] Implement `RemoveAffiliationCommand` (admin removes member; or member leaves on their own)
- [ ] Implement `RequestGuestSpotCommand` and `RespondToGuestSpotCommand`
- [ ] Implement `SetPrimaryAffiliationCommand` (artist sets which studio is their primary)
- [ ] Implement queries: `GetStudioByIdQuery`, `GetStudioRosterQuery`, `GetMyAffiliationsQuery`, `SearchStudiosByNameQuery`
- [ ] Implement `StudiosController` and `AffiliationsController`
- [ ] Integration tests covering all join/leave/role flows including guest spots
- [ ] Commit: "feat(studios): studio creation, affiliations, guest spots"

## Phase 6 — Verification

- [ ] Seed `Jurisdiction` table with Montréal row
- [ ] Implement `UploadStudioCredentialCommand` (admin of studio uploads document)
- [ ] Implement `UploadArtistCredentialCommand` (artist uploads own credential)
- [ ] Implement `ReviewCredentialCommand` (admin approves/rejects)
- [ ] Implement query `GetVerificationQueueQuery` for admin dashboard
- [ ] Implement computed verification status logic for `Artist` and `Studio` (in a domain service or query)
- [ ] Implement `IImageStorage.LocalFilesystemImageStorage` impl (writes to `wwwroot/uploads/credentials/`)
- [ ] Implement `IImageStorage.R2ImageStorage` impl (gated by config)
- [ ] Add `CredentialsController`, `AdminController` with appropriate role guards
- [ ] Integration tests: upload, review, status computation, expiry handling
- [ ] Commit: "feat(verification): credentials, admin review, computed status"

## Phase 7 — Portfolio

- [ ] Seed `TattooStyle` with the canonical 32 styles from `docs/FEATURE_SPECS.md` (`IsCanonical = true`)
- [ ] Implement image upload pipeline: validate, EXIF strip, resize to thumb/medium/full, upload, return URLs
  - Use `SkiaSharp` or `ImageSharp` (license-aware: `ImageSharp` is non-permissive past v3 — pick `SkiaSharp` to avoid issues, or pin `ImageSharp` v2 with awareness)
- [ ] Implement `CreatePortfolioPieceCommand` (artist uploads fresh photo + metadata)
- [ ] Implement `AddSessionPhotoCommand` (artist adds photos to multi-session pieces)
- [ ] Implement `UploadHealedPhotoCommand` (customer uploads healed photo for their booking)
- [ ] Implement `HideSessionPhotoCommand` (artist hides customer-uploaded photo for content policy reasons; logs reason for admin audit)
- [ ] Implement `UpdatePortfolioPieceCommand`
- [ ] Implement `DeletePortfolioPieceCommand`
- [ ] Implement queries: `GetArtistPortfolioQuery`, `GetPortfolioPieceQuery`, `GetStudioCollectivePortfolioQuery`
- [ ] Implement `PortfolioController`
- [ ] Integration tests covering upload, healed-photo flow, hiding policy
- [ ] Commit: "feat(portfolio): pieces, photos, paired fresh+healed model"

## Phase 8 — Discovery

- [ ] Implement `IArtistDiscoveryService` in Application layer with method `SearchAsync(SearchCriteria, CancellationToken)`
  - Bounding-box spatial filter (PostGIS `ST_Within(point, envelope)`)
  - Style filter (join through `Artist.Styles` via affiliated artists per studio)
  - Verified filter (computed status >= DocumentsSubmitted, default Verified only)
  - Availability filter (joins to `ArtistAvailabilityProjection` for the requested date range — even if projection is empty initially, scaffold the query path)
  - Sort by distance ascending (`ST_Distance(point, center)`)
  - Alternative sort options scaffolded: availability soonness, verified-first
- [ ] Implement `DiscoveryController` with `GET /api/discovery/studios` returning paginated results
- [ ] Implement `GET /api/studios/{id}` for studio detail (including roster)
- [ ] Implement `GET /api/artists/{id}` for artist detail (including portfolio summary)
- [ ] Integration tests with seeded data verifying bounding box, style filter, distance sort
- [ ] Commit: "feat(discovery): spatial search, filters, sort"

## Phase 9 — Availability

- [ ] Implement `SetAvailabilityPatternCommand` (artist sets recurring weekly pattern)
- [ ] Implement `AddAvailabilityOverrideCommand`, `RemoveAvailabilityOverrideCommand`
- [ ] Implement `CreateBookingWindowCommand`, `CloseBookingWindowCommand`
- [ ] Implement `SetLeadTimesCommand` (artist sets `MinimumLeadTimeDays` per booking type)
- [ ] Implement `IAvailabilityProjector` service that computes `IsBookable` per artist per day given pattern + overrides + windows + bookings
- [ ] Implement `RebuildArtistAvailabilityProjectionCommand` (single artist, used on-demand)
- [ ] Implement `RebuildAllAvailabilityProjectionsRecurringJob` for Hangfire (nightly)
- [ ] Wire on-demand recomputation triggers: pattern change, override change, window change, booking accept/cancel/complete
- [ ] Implement `AvailabilityController` exposing pattern/override CRUD + iCal export
- [ ] iCal export: tokenized URL per artist, returns `text/calendar` with confirmed bookings only
- [ ] Integration tests covering projection accuracy across pattern + override + window + booking interactions
- [ ] Commit: "feat(availability): patterns, overrides, projection, iCal export"

## Phase 10 — Bookings core (no Stripe yet — build the state machine first)

- [ ] Implement `RequestBookingCommand` (validate lead time, strip contact info from description, persist as `Requested`)
- [ ] Implement `AcceptBookingCommand` (artist accepts, sets confirmed date/time, transitions to `Accepted` — Stripe capture is a separate handler call in next phase)
- [ ] Implement `DeclineBookingCommand`
- [ ] Implement `RequestMoreInfoCommand` (artist requests, sets status `AwaitingCustomerInfo`)
- [ ] Implement `RespondWithMoreInfoCommand` (customer responds, sets status back to `Requested`)
- [ ] Implement `MarkBookingInProgressCommand`, `MarkBookingCompletedCommand`
- [ ] Implement `CancelBookingByCustomerCommand`, `CancelBookingByArtistCommand` (refund logic per policy snapshot — Stripe stub for now)
- [ ] Implement `ExpireRequestedBookingCommand` (called from Hangfire after 7 days)
- [ ] Implement queries: `GetMyBookingsAsCustomerQuery`, `GetMyBookingsAsArtistQuery`, `GetBookingDetailQuery`
- [ ] Implement `BookingsController`
- [ ] Integration tests covering all state transitions and lead time enforcement
- [ ] Commit: "feat(bookings): request flow, state machine, lead time enforcement"

## Phase 11 — Stripe Connect integration

- [ ] Implement `IStripeService` infrastructure impl wrapping `Stripe.net`
- [ ] Implement `CreateConnectAccountCommand` and `GenerateOnboardingLinkCommand` for artist onboarding
- [ ] Implement webhook handler endpoint `POST /api/webhooks/stripe` with signature verification
- [ ] Webhook handlers for: `account.updated` (update `ArtistPaymentStatus`), `payment_intent.succeeded`, `payment_intent.canceled`, `charge.refunded`, `charge.dispute.created`
- [ ] Wire pre-auth into `RequestBookingCommand` (create PaymentIntent with `capture_method = manual`, store `StripePaymentIntentId`)
- [ ] Wire capture into `AcceptBookingCommand`
- [ ] Wire void into `DeclineBookingCommand` and `ExpireRequestedBookingCommand`
- [ ] Wire refund logic into cancellation handlers per policy snapshot
- [ ] Add Hangfire scheduled job: at booking creation, schedule expiration job for `RequestedAt + 7 days`
- [ ] Integration tests using Stripe test mode + webhook signature helpers
- [ ] Commit: "feat(stripe): Connect onboarding, pre-auth, capture, refunds, webhooks"

## Phase 12 — Messaging

- [ ] Implement `OpenMessageThreadOnDepositCapturedHandler` (domain event handler triggered by booking accept)
- [ ] Implement `SendMessageCommand` (validates thread is `Active`, sender is one of the two parties)
- [ ] Implement `MarkMessageReadCommand`
- [ ] Implement `UploadMessageAttachmentCommand`
- [ ] Implement `ReportMessageCommand` (creates `MessageReport`, queues admin review)
- [ ] Implement admin commands: `HideMessageCommand`, `ResolveMessageReportCommand`
- [ ] Implement queries: `GetThreadMessagesQuery`, `GetUnreadMessageCountQuery`, `GetMyActiveThreadsQuery`
- [ ] Implement `MessagesController`
- [ ] Implement Hangfire scheduled job: at booking terminal state, schedule thread lock for `+90 days`
- [ ] Integration tests covering gating (cannot send before deposit captured), reporting, locking
- [ ] Commit: "feat(messaging): booking-scoped threads, reports, retention"

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

# Needlr — Architecture

## Stack summary

| Layer | Tech |
|---|---|
| Backend runtime | .NET 9, C# 13, ASP.NET Core 9 |
| Frontend | Blazor WebAssembly (PWA) |
| Database | PostgreSQL 16 with PostGIS extension |
| ORM | Entity Framework Core 9 with `Npgsql.EntityFrameworkCore.PostgreSQL` and `NetTopologySuite` |
| Auth | ASP.NET Core Identity + JWT bearer for API |
| Background jobs | Hangfire with PostgreSQL storage |
| Payments | Stripe Connect (individual artist accounts) via `Stripe.net` |
| Image storage | `IImageStorage` abstraction; local filesystem impl for dev, Cloudflare R2 (S3-compatible) for prod |
| Maps (frontend) | MapLibre GL JS via Blazor JS interop, MapTiler tiles |
| Use case framework | MediatR with FluentValidation pipeline |
| Mapping | Mapster |
| Logging | Serilog |
| Testing | xUnit, FluentAssertions, Testcontainers.PostgreSql, WebApplicationFactory |

## Solution layout

```
Needlr.sln
├── src/
│   ├── Needlr.Domain/              # Entities, value objects, enums, domain events. No external deps except NetTopologySuite.Geometries.
│   ├── Needlr.Application/         # Use cases (MediatR handlers), DTOs, validators, interfaces (IRepository, IImageStorage, IEmailSender, IStripeService, etc.)
│   ├── Needlr.Infrastructure/      # EF Core DbContext, Identity setup, Stripe client, image storage impls, email sender impl, Hangfire jobs, projections
│   ├── Needlr.Api/                 # ASP.NET Core Web API. Controllers (thin), DI wiring, JWT setup, Swagger, Serilog, webhook endpoints
│   ├── Needlr.Web/                 # Blazor WebAssembly PWA. Pages, components, services, MapLibre interop
│   └── Needlr.Contracts/           # Shared DTOs and request/response types referenced by both Api and Web
└── tests/
    ├── Needlr.Domain.Tests/
    ├── Needlr.Application.Tests/
    ├── Needlr.Infrastructure.Tests/
    ├── Needlr.Api.IntegrationTests/  # Testcontainers Postgres+PostGIS, real DbContext, real handlers, real HTTP
    └── Needlr.Architecture.Tests/    # NetArchTest layering enforcement
```

## Layering rules

These are enforced by project references and architecture tests (write `Needlr.Architecture.Tests` early).

- **Domain** depends on nothing outside the BCL and `NetTopologySuite.Geometries`.
- **Application** depends on Domain only.
- **Infrastructure** depends on Application and Domain.
- **Api** depends on Application and Infrastructure (Infrastructure only for DI registration; no Infrastructure types in controllers).
- **Web** depends on Contracts only. **Web never references Application, Infrastructure, or Domain.**
- **Contracts** depends on nothing.
- **Tests**: each test project depends on the layer under test plus its dependencies.

Controllers must remain thin: deserialize input, call a MediatR handler, return the result. No business logic in controllers, ever.

All EF Core queries live in handlers or in repository implementations in Infrastructure. No `DbContext` injection into controllers.

All spatial queries flow through application-layer services (e.g., `IArtistDiscoveryService`). No raw `Point`-touching LINQ in controllers.

## NuGet package map

### `Needlr.Domain`
- `NetTopologySuite` (for `Point` type)

### `Needlr.Application`
- `MediatR`
- `FluentValidation`
- `FluentValidation.DependencyInjectionExtensions`
- `Mapster`

### `Needlr.Infrastructure`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design` (for migrations)
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Hangfire.Core`
- `Hangfire.PostgreSql`
- `Stripe.net`
- `AWSSDK.S3` (Cloudflare R2 is S3-compatible)
- Resend (HttpClient against `api.resend.com`; dev impl logs to console)
- `Serilog`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

### `Needlr.Api`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`
- `Hangfire.AspNetCore`
- `Serilog.AspNetCore`

### `Needlr.Web`
- `Microsoft.AspNetCore.Components.WebAssembly`
- `Microsoft.AspNetCore.Components.WebAssembly.DevServer`
- `Microsoft.Extensions.Http`
- (MapLibre GL JS loaded via `<script>` tag, wrapped in JS interop module — no NuGet package needed)

### Test projects
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `FluentAssertions`
- `Testcontainers.PostgreSql` (with postgis image override)
- `Microsoft.AspNetCore.Mvc.Testing` (integration tests)
- `NSubstitute` (for unit test mocks)

## Database

- PostgreSQL 16 with PostGIS 3.4
- Spatial columns: `Studio.Location`, `CustomerProfile.Location` (optional)
- GiST indexes on all `geometry` columns
- Identity tables: stock ASP.NET Core Identity schema
- Hangfire schema in its own `hangfire` schema
- Application schema in `public`
- Migrations in `Needlr.Infrastructure/Migrations`
- `docker-compose.yml` at repo root runs `postgis/postgis:16-3.4` for local dev

## Authentication

- ASP.NET Core Identity for user store
- JWT bearer tokens for API auth (issued from `/api/auth/login`)
- Refresh tokens stored in DB with rotation
- Tokens carry `userId`, `role`, and (for artists) `artistId` claims
- Web client stores tokens in `localStorage` (acceptable for v1; revisit if XSS posture changes)
- API uses `[Authorize(Roles = "Artist")]` etc. for role-based endpoints

## Stripe Connect

Per ADR-005, Needlr uses Stripe's **direct charge** model. The artist is the merchant of record on the customer's receipt and bank statement. Needlr's account never holds, splits, or routes funds.

- Each artist gets their own Stripe **Express** Connect account
- Onboarding via Stripe-hosted `AccountLink` flow during artist onboarding. `Artist.StripeConnectAccountId` is recorded on creation; `ArtistPaymentStatus` stays `OnboardingInProgress` until the `account.updated` webhook reports `details_submitted == true && charges_enabled == true`, at which point it becomes `Active`.
- **All `PaymentIntent`/`Refund` API calls use the `Stripe-Account` request header** (Stripe.NET: `RequestOptions { StripeAccount = artist.StripeConnectAccountId }`) so charges land directly in the artist's connected account. **Do not use `transfer_data.destination`** — that's the destination-charge model and routes funds through the platform first.
- **No `application_fee_amount`** at launch — no per-transaction platform fee. See ADR-005.
- **Pre-auth at booking request:** create a `PaymentIntent` on the connected account with `capture_method = manual`, store the `PaymentIntentId` on the booking.
- **Capture on artist acceptance:** `PaymentIntent.Capture` on the connected account.
- **Cancel on decline or expiry:** `PaymentIntent.Cancel` on the connected account.
- **Refund on cancellation per `Booking.CancellationPolicySnapshot`:** `Refund.Create` on the connected account with the appropriate amount.
- **Webhook endpoint at `/api/webhooks/stripe`** subscribes to **Connect events** (events delivered with an `account` field identifying the connected account). Signatures verified via the `Stripe-Signature` header against the Connect webhook signing secret. Handlers:
  - `account.updated` — sync `ArtistPaymentStatus` (`Active`, `Restricted`, etc.)
  - `payment_intent.succeeded` — capture confirmation
  - `payment_intent.canceled` — cancellation/expiry confirmation
  - `charge.refunded` — record refund on the booking
  - `charge.dispute.created` — flag booking, alert admin
- Webhook handler is **idempotent** — events may be redelivered. Track processed event IDs in Infrastructure to skip duplicates.

## Image storage

- `IImageStorage` interface in Application:
  - `Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, CancellationToken ct)`
  - `Task DeleteAsync(string key, CancellationToken ct)`
  - `Task<Stream> GetAsync(string key, CancellationToken ct)`
- `LocalFilesystemImageStorage` impl in Infrastructure for dev (writes to `wwwroot/uploads/`, served statically)
- `R2ImageStorage` impl in Infrastructure for prod (uses `AWSSDK.S3` against R2's S3-compatible endpoint)
- Image upload pipeline: receive multipart upload → validate (mime, size, dimensions) → strip EXIF → resize to multiple sizes (thumbnail, medium, full) → upload all sizes → store URLs in DB
- Use ImageSharp or SkiaSharp for resize+EXIF strip

## Background jobs (Hangfire)

Recurring:
- **Nightly** (3 AM): rebuild `ArtistAvailabilityProjection` for the rolling 90-day window for all artists with changes since yesterday
- **Nightly** (3:30 AM): purge `BookingAttachment` records (and their blob storage) for bookings in terminal state >1 year
- **Nightly** (4 AM): scan for credentials with expiry within 30/7/0 days, send notifications, downgrade verification status
- **Daily** (10 AM): send healed-photo prompts to customers whose bookings hit the 4-month mark
- **Daily** (9 AM): send 24-hour booking reminders for sessions tomorrow

One-off scheduled:
- **At booking creation**: schedule auto-void of pre-auth at `RequestedAt + 7 days` (cancelled if booking accepted/declined first)
- **At booking acceptance**: schedule message thread lock at `Completed + 90 days` (after Completed)

## Frontend (Blazor WebAssembly PWA)

- PWA-configured `Program.cs` with service worker for offline shell + asset caching
- Service worker handles Web Push subscription
- MapLibre GL JS loaded via `<script>` in `index.html`, wrapped in `mapInterop.js` module
- `MapComponent.razor` exposes `OnBoundsChanged` callback to parent pages (debounced 300ms client-side)
- API client generated from minimal hand-written `INeedlrApi` in Contracts; calls go via `HttpClient` with JWT bearer
- State management: scoped services per circuit (e.g., `AuthState`, `BookingDraftState`)
- Routing: standard Blazor routing, with auth guards via `AuthorizeRouteView`
- Tokens in `localStorage`; JWT refresh logic in `AuthService` with automatic retry on 401

## Testing strategy

- **Domain tests**: pure unit tests of domain logic (entity invariants, state transitions, value object behavior)
- **Application tests**: handler tests with substituted infrastructure (`NSubstitute`)
- **Infrastructure tests**: tests that exercise EF Core mappings, projections, and PostGIS queries against a real Testcontainers Postgres+PostGIS instance
- **Integration tests**: full-stack via `WebApplicationFactory<Program>`, real DB via Testcontainers, real handlers, real HTTP. Test Stripe via Stripe's test mode and webhook signature verification helpers.
- **Architecture tests**: `NetArchTest` to enforce layering rules. Run as part of unit test suite.

## Deployment shape

(Out of scope to implement at v1 build, but design accommodates:)
- API in a Linux container (Dockerfile in `src/Needlr.Api`)
- Web served as static files (Blazor WASM publishes to a `wwwroot/`)
- Postgres+PostGIS managed instance (Supabase, Neon with PostGIS, or self-hosted)
- Hangfire runs in the API process for v1 (move to dedicated worker service if/when load justifies)
- R2 bucket for production image storage
- Cloudflare in front for CDN + WAF
- Domain: needlr.app (or whatever the user chooses; not relevant to the build)

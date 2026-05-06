# Needlr

A location-first tattoo portfolio and booking platform for Montréal — the deliberate anti-Instagram for tattoo discovery.

The full product context lives in [`docs/`](./docs):

- [`PRODUCT_BRIEF.md`](./docs/PRODUCT_BRIEF.md) — what we're building and why
- [`ARCHITECTURE.md`](./docs/ARCHITECTURE.md) — tech stack and layering rules
- [`DOMAIN_MODEL.md`](./docs/DOMAIN_MODEL.md) — entities and enums
- [`FEATURE_SPECS.md`](./docs/FEATURE_SPECS.md) — feature-level decisions
- [`BUILD_PLAN.md`](./docs/BUILD_PLAN.md) — ordered execution plan
- [`docs/adr/`](./docs/adr) — architectural decision records (binding)
- [`CLAUDE.md`](./CLAUDE.md) — conventions and hard constraints

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (9.0.313 or newer in the same feature band — see [`global.json`](./global.json))
- [Docker](https://docs.docker.com/get-docker/) — for the local Postgres + PostGIS container
- _(optional)_ A Stripe test account if you want to exercise the real card-collection flow; otherwise the FE falls back to a synthetic payment-method id and the test fixture's `FakeStripeService` covers the server side.

## Quick start

```bash
# 1. Start Postgres + PostGIS
docker compose up -d

# 2. Restore and build the solution
dotnet restore
dotnet build

# 3. Apply EF Core migrations
dotnet ef database update \
  --project src/Needlr.Infrastructure \
  --startup-project src/Needlr.Api

# 4. Run the API
dotnet run --project src/Needlr.Api

# 5. (separate terminal) Run the Blazor PWA client
dotnet run --project src/Needlr.Web
```

## Configuration

Sensitive values come from `appsettings.{Environment}.Local.json` (gitignored) or environment variables. Never commit secrets.

| Section | Key | Notes |
|---|---|---|
| `ConnectionStrings` | `Postgres` | Defaults to the `docker-compose.yml` container; override for hosted DBs |
| `Jwt` | `SigningKey` | 32+ bytes; rotate by republishing JWTs (refresh-token chain handles this) |
| `Jwt` | `Issuer`, `Audience`, `AccessTokenLifetimeMinutes`, `RefreshTokenLifetimeDays` | Standard JWT config |
| `Stripe` | `SecretKey` | Set to enable Connect onboarding + capture; without it `IStripeService` doesn't register |
| `Stripe` | `ConnectWebhookSigningSecret` | For verifying inbound `/api/webhooks/stripe` payloads |
| `Stripe` | `OnboardingReturnUrl`, `OnboardingRefreshUrl` | Where Stripe sends artists after the hosted KYC flow |
| `Hangfire` | `EnableServer` | `true` to start the recurring-job worker + dashboard at `/hangfire` |
| `ImageStorage` | `Backend` | `Local` (default) or `R2` |
| `ImageStorage` | `LocalRootPath` | Where the local backend writes blobs (default: `wwwroot/uploads`) |
| `Notifications` | `SendGridApiKey` | Optional; without it `IEmailSender` logs to the console |
| `Notifications` | `VapidPublicKey`, `VapidPrivateKey`, `VapidSubject` | Optional Web Push config |

The Blazor client reads its own `wwwroot/appsettings.json`:

| Section | Key | Notes |
|---|---|---|
| `Api` | `BaseUrl` | API origin; defaults to the WASM host (same domain in prod) |
| `Stripe` | `PublishableKey` | `pk_test_…` or `pk_live_…`; empty key uses synthetic payment-method ids |

## Tests

```bash
# Full test suite (Domain + Application + Infrastructure + Architecture + Integration).
# Integration tests pull the postgis/postgis Docker image on first run.
dotnet test
```

The integration suite uses `Testcontainers.PostgreSql` against the `postgis/postgis:16-3.4` image. `WebAppFixture` wires the test host with deterministic test doubles for Stripe, push, email, Hangfire schedulers, and thread-lock scheduling — see [`tests/Needlr.Api.IntegrationTests/Fixtures/`](./tests/Needlr.Api.IntegrationTests/Fixtures).

End-to-end smoke: [`HappyPathTests`](./tests/Needlr.Api.IntegrationTests/EndToEnd/HappyPathTests.cs) walks customer signup → artist signup → booking request → accept → webhook → message exchange → completion → feedback in one stitched test.

## Solution layout

```
Needlr.sln
├── src/
│   ├── Needlr.Domain/          # Entities, value objects, enums (no external deps)
│   ├── Needlr.Application/     # MediatR handlers, DTOs, validators, abstractions
│   ├── Needlr.Infrastructure/  # EF Core, Identity, Stripe, image storage, Hangfire
│   ├── Needlr.Api/             # ASP.NET Core Web API (thin controllers)
│   ├── Needlr.Web/             # Blazor WebAssembly PWA
│   └── Needlr.Contracts/       # Shared DTOs between Api and Web (incl. INeedlrApi)
└── tests/
    ├── Needlr.Domain.Tests/
    ├── Needlr.Application.Tests/
    ├── Needlr.Infrastructure.Tests/
    ├── Needlr.Api.IntegrationTests/   # Testcontainers Postgres + PostGIS
    └── Needlr.Architecture.Tests/     # NetArchTest layering enforcement
```

Layering rules and project references are documented in [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) § Layering rules.

## Conventions

See [`CLAUDE.md`](./CLAUDE.md) for code style and the five hard product constraints.

The ADRs in [`docs/adr/`](./docs/adr) are binding product invariants:

1. ADR-001 — No social features
2. ADR-002 — No public reviews
3. ADR-003 — Message privacy and booking-scoped messaging
4. ADR-004 — Artist-managed studios (no `StudioOwner` role)
5. ADR-005 — Individual Stripe Connect accounts only

## Contributing

- One type per file. File-scoped namespaces. Nullable refs on. `TreatWarningsAsErrors` on for non-test projects.
- MediatR handlers carry the business logic; controllers are thin.
- Tests in [`Needlr.Architecture.Tests`](./tests/Needlr.Architecture.Tests) enforce the layering. They will fail your PR if Domain depends on anything beyond `NetTopologySuite.Geometries`.
- New features go in [`docs/FEATURE_SPECS.md`](./docs/FEATURE_SPECS.md) and the relevant phase in [`docs/BUILD_PLAN.md`](./docs/BUILD_PLAN.md) before being built. Don't introduce undocumented features.
- Commits: conventional-commits style (`feat(scope): …`, `chore: …`, etc.).

## Deployment shape (v1)

Out of scope to script in v1, but the architecture accommodates:

- API in a Linux container (a `Dockerfile` would live in `src/Needlr.Api`)
- Web served as static files (Blazor WASM publishes to `wwwroot/`)
- Postgres + PostGIS as a managed instance (Supabase / Neon-with-PostGIS / self-hosted)
- Hangfire runs in-process in the API for v1; move to a dedicated worker if load justifies
- Cloudflare R2 (S3-compatible) for production image storage; Cloudflare in front for CDN + WAF

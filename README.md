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
- _(optional)_ [Node.js](https://nodejs.org) — only needed if/when we add frontend tooling later

## Quick start

```bash
# 1. Start Postgres + PostGIS
docker compose up -d

# 2. Restore and build the solution
dotnet restore
dotnet build

# 3. Apply EF Core migrations (available from Phase 2 onward)
dotnet ef database update \
  --project src/Needlr.Infrastructure \
  --startup-project src/Needlr.Api

# 4. Run the API
dotnet run --project src/Needlr.Api

# 5. (separate terminal) Run the Blazor PWA client
dotnet run --project src/Needlr.Web
```

## Solution layout

```
Needlr.sln
├── src/
│   ├── Needlr.Domain/          # Entities, value objects, enums (no external deps)
│   ├── Needlr.Application/     # MediatR handlers, DTOs, validators, abstractions
│   ├── Needlr.Infrastructure/  # EF Core, Identity, Stripe, image storage, Hangfire
│   ├── Needlr.Api/             # ASP.NET Core Web API (thin controllers)
│   ├── Needlr.Web/             # Blazor WebAssembly PWA
│   └── Needlr.Contracts/       # Shared DTOs between Api and Web
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

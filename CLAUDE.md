# CLAUDE.md — Needlr

This file is loaded into every Claude Code session for this repository. Read it before doing anything. If you find yourself about to make a decision that contradicts this file, stop and ask.

## What Needlr is

Needlr is a location-first tattoo portfolio and booking platform for Montréal. It is the deliberate anti-Instagram for tattoo discovery: a tool for finding licensed local artists and booking sessions with them, with the social-network mechanics removed. The full product rationale is in `docs/PRODUCT_BRIEF.md`.

## Required reading before substantial changes

- `docs/PRODUCT_BRIEF.md` — what we're building and why
- `docs/DOMAIN_MODEL.md` — entities, relationships, enums
- `docs/FEATURE_SPECS.md` — feature-level decisions and behaviors
- `docs/ARCHITECTURE.md` — tech stack and layering rules
- `docs/BUILD_PLAN.md` — the ordered execution plan; check off items as you complete them
- `docs/adr/` — architectural decision records; these are binding

## Hard constraints (non-negotiable product invariants)

These have ADRs explaining the reasoning. Do not violate them. Do not add features that re-introduce them under a different name.

1. **No social features.** No likes, comments, shares, follows, reposts, feeds, activity streams, or "trending" anything. See ADR-001.
2. **No public reviews.** No stars, no public ratings, no public testimonials, no thumbs up/down. Private structured feedback to Needlr admins only. Behavioral signals on profiles are computed from platform data, never from customer opinions. See ADR-002.
3. **Messages are private and booking-scoped.** No DMs, no freeform pre-booking chat. A customer cannot message an artist until a booking is accepted and the deposit is captured. Messages are readable by Needlr admins only in response to a report or legal request. See ADR-003.
4. **Studios are artist-managed.** There is no `StudioOwner` role. Studio admin rights are a per-affiliation permission held by an artist. See ADR-004.
5. **All money flows to individual artist Stripe Connect accounts.** Needlr never holds funds, never splits funds, never operates a studio-level account. See ADR-005.

If a feature request, refactor, or "nice to have" idea would violate any of these, it does not get built. Surface the conflict to the user.

## Hard constraints (technical)

- **.NET 9, C# 13.** File-scoped namespaces. Nullable reference types enabled. Implicit usings on. `TreatWarningsAsErrors` on for all non-test projects.
- **Clean Architecture layering** (Domain → Application → Infrastructure → Api, plus Web client and shared Contracts). Domain has zero external dependencies. Application depends only on Domain. Controllers are thin — all business logic lives in MediatR handlers in Application. See `docs/ARCHITECTURE.md`.
- **EF Core 9 with Npgsql + NetTopologySuite.** All spatial data uses `NetTopologySuite.Geometries.Point`. All spatial queries flow through application-layer services, never directly from controllers.
- **MediatR for use cases.** One handler per use case. Validators via FluentValidation, registered in the MediatR pipeline.
- **Records for DTOs.** Sealed classes by default. Primary constructors where they read well.
- **xUnit + FluentAssertions.** Integration tests use `Testcontainers.PostgreSql` with the postgis image. No in-memory EF provider for tests that touch spatial queries — it doesn't support PostGIS.

## Code style

- One type per file. Filename matches type name.
- `using` directives inside the namespace block are not used; top-of-file is fine.
- Async methods end in `Async` and accept `CancellationToken` as the last parameter.
- No `var` for primitive or well-known types where the type isn't obvious from the right-hand side. `var` is fine for `new Foo()` or LINQ chains.
- Prefer guard clauses over nested conditionals.
- Public APIs documented with XML doc comments. Private methods documented only when the intent isn't obvious from the name.

## Working with this codebase

- **Check `docs/BUILD_PLAN.md` for the next task.** Tasks are ordered. Do not skip ahead unless explicitly instructed.
- **After each significant unit of work, stop and summarize what changed before moving on.** This is a long-running build; the user needs to be able to follow along.
- **Run tests before declaring a step complete.** A step that doesn't have tests passing isn't done.
- **Update `docs/BUILD_PLAN.md`** to check off completed items as you go.
- **If you discover the docs are wrong or incomplete, update the docs in the same change.** The docs are the source of truth, not your memory of what was discussed.
- **Never add a NuGet package without noting it in your summary.** Package choices are architecture decisions.
- **Never add a feature not in `docs/FEATURE_SPECS.md`.** If the user asks for one, suggest adding it to the spec first.

## What this project is not

- Not a social network
- Not a review platform
- Not a payments/payroll/revenue-split intermediary
- Not a multi-jurisdiction product at launch (Montréal only; jurisdiction entity exists for v2)
- Not a native mobile app at launch (PWA only)
- Not a real-time chat app (async messaging only in v1)

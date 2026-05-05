# ADR-004: Artist-managed studios, no StudioOwner role

**Status:** Accepted
**Date:** Project inception

## Context

Tattoo studios in the real world have varied ownership structures:
- A single tattooing artist who owns and runs the shop
- Multiple tattooing partners, one of whom handles business
- A non-tattooing owner (often a spouse, family member, or business operator) who manages the shop while artists tattoo
- Franchised or chained shops with corporate ownership
- Solo artists with no brick-and-mortar (working out of home studios or private spaces)
- Private/invite-only studios with collective ownership

A general-purpose model would have a `StudioOwner` role distinct from `Artist`, with its own onboarding flow, permissions, and Stripe Connect setup. This adds significant complexity: separate onboarding paths, role-based UI, Stripe account routing decisions, revenue-split logic if owners take cuts from artists.

For Needlr's v1 launch in Montréal, the user has decided to scope the product to artist-managed studios only. Mike's non-tattooing spouse, in the planning conversation example, would not have a Needlr account — Mike's affiliated artists would manage the shop's Needlr presence themselves.

## Decision

Needlr has three roles: `Customer`, `Artist`, `Admin`. There is no `StudioOwner` role.

Studios are managed by artists. Specifically:

- A studio is created by an artist during their onboarding flow. That artist is the studio's `Founder`.
- Studio admin rights are a per-affiliation permission level, not a separate role. The `ArtistStudioAffiliation` entity has a `Role` field with values `Founder`, `Admin`, or `Member`.
- An `Admin` (which `Founder` always implies) can: edit studio info, upload studio credentials, manage join policy, invite/approve/remove artists, promote members to admin.
- A `Founder` cannot be removed except by ceding founder status to another admin first.
- A studio's `JoinPolicy` (`Open` / `InviteOnly` / `Closed`) controls how new artists join.
- Solo and Private studios are studios with a single member who is by default Founder + Admin.

All money flows directly to individual artist Stripe Connect accounts. No studio-level Stripe accounts. See ADR-005.

## Consequences

**Positive:**
- One onboarding flow for the supply side (Artist onboarding), which dramatically simplifies the product.
- No role-switching UI ("am I in artist mode or owner mode?").
- No need to model or enforce revenue splits between owner and artists.
- No risk of becoming a payroll intermediary.
- Solo artists and shop-affiliated artists use the same flows with minor variations.
- Aligned with the "focus on the craft" ethos — Needlr is a tool for tattoo artists, not for tattoo businesses.

**Negative:**
- Traditional shops with a non-tattooing owner who handles all business operations cannot use Needlr in the way they currently operate. They need a tattooing partner willing to be the studio admin, or the non-tattooing owner needs to operate a "virtual artist" account, or they don't use Needlr.
- This cuts us off from a slice of the market — particularly more corporate/franchised shops. Acceptable for a v1 Montréal launch where the target audience is independent artists and small shops.
- Studio-level operational features that a dedicated business operator might want (analytics, accounting exports, multi-shop management) are not in scope. A v2 `StudioOwner` role might re-emerge if we expand to chains.

## Implementation notes

- The `Studio.CreatedByArtistId` field is informational; it tracks the original founder for audit purposes but the active permission is on the affiliation.
- When the founder leaves the studio, they must cede founder status to another admin. If no other admins exist, the studio either gets a new admin appointed (existing member promoted) or is closed.
- A single artist can be affiliated with multiple studios (primary + guest spots). They can be `Admin` at one and `Member` at another. Permissions are per-affiliation.

## Defending against future erosion

Likely future arguments for adding a StudioOwner role:

- "Big shops want their non-tattooing manager to have access" — for v1 they can either operate via an artist-admin or not use Needlr. If real demand emerges in v2+, design a `StudioOperator` role that has admin permissions on the studio without being an artist (no portfolio, no Stripe Connect, no booking inbox).
- "Owners want to take a cut of artist deposits" — explicitly out of scope. See ADR-005. Real-world cuts are handled off-platform between the owner and the artist.
- "Owners want studio-level analytics" — can be served by a future `StudioOperator` role; not justified at v1.

## Related

- ADR-005: Individual Stripe Connect only
- `docs/FEATURE_SPECS.md` § Studios, § Onboarding
- `docs/DOMAIN_MODEL.md` § Studio, § ArtistStudioAffiliation

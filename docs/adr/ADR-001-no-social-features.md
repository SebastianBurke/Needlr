# ADR-001: No social features

**Status:** Accepted
**Date:** Project inception

## Context

Tattoo discovery and booking today happens primarily on Instagram. The user experience is bad on both sides — for customers because Instagram lacks structured discovery (location, style, availability, license filters), for artists because the platform's social-network mechanics (algorithmic feed, likes, comments, follower counts) impose a content-treadmill that competes with the actual craft of tattooing.

Needlr exists specifically to break this dynamic. The product hypothesis is that tattoo artists and tattoo customers want a tool, not a network.

## Decision

Needlr will not have any social-network features. Specifically, we will not build, and will actively reject proposals to build:

- Likes, hearts, reactions, or any quantified positive signal on portfolio pieces or artists
- Public comments on portfolio pieces, artists, studios, or any other entity
- Shares, reposts, or any cross-account propagation mechanic
- Follow/follower relationships between users
- A feed, timeline, "for you" surface, or any algorithmically curated stream
- "Trending" or "popular" surfaces of any kind
- Public follower counts or any vanity metric
- Activity streams (X did Y) of any kind

## Consequences

**Positive:**
- The product remains aligned with its founding hypothesis. The thing that makes Needlr different from Instagram stays different.
- Artists are not pressured into a content-production loop. They post portfolio pieces because they did the work, not because they need engagement.
- Customers are not exposed to popularity-as-quality-signal. Discovery is driven by genuine fit (location, style, availability, healed work, verification) instead of by who has the most followers.
- The data model is simpler. No notification fanout for likes/comments. No moderation queue for public comments. No anti-abuse system for like-bombing or follower fraud.

**Negative:**
- Artists who currently maintain large Instagram followings cannot transfer that social capital to Needlr. This is by design but will create resistance from established artists in early adoption.
- Customers conditioned to "X followers means good" will lack a familiar quality signal. We compensate with the healed-photo portfolio, behavioral signals, and verification tiers (see ADR-002 and the feature specs).
- Some legitimate uses of "social" features (e.g., a customer wanting to publicly thank an artist) have no in-product home. This is an accepted tradeoff.

## Defending against future erosion

This decision will be tested. Common arguments for re-introducing social mechanics will appear over time:

- "Just a heart button, it's barely social" — no. Hearts become counts become rankings become an algorithmic feed. Re-litigates the entire decision.
- "Comments would help customers ask quick questions" — no. The booking-request form is the structured first contact. Public comments re-introduce the parasocial DM dynamic in a different surface.
- "Followers would help retain users" — no. Retention via lock-in is anti-product. Customers should return because Needlr finds them better artists, not because they have followers to maintain.
- "Just for studios, not artists" — no. Studio likes/comments cascade pressure onto artists.

The bar to overturn this ADR is: a written, dated proposal showing that the absence of the feature has materially harmed the booking metric (the only metric that matters), and that the proposed feature is the smallest possible change that addresses the harm. Even then, prefer non-social alternatives.

## Related

- ADR-002: No public reviews
- ADR-003: Message privacy
- `docs/PRODUCT_BRIEF.md` — the founding rationale

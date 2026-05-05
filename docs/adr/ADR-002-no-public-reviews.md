# ADR-002: No public reviews

**Status:** Accepted
**Date:** Project inception

## Context

Marketplaces conventionally use public reviews (stars + text) to provide trust, quality, fit, and accountability signals to prospective customers. Yelp, Google, Booksy, StyleSeat all do this. The pattern is industry-default in the beauty and personal services space.

Public reviews carry significant downsides specific to the tattoo context:

1. **Tattoos are art commissions, not services.** A customer's satisfaction depends partly on factors the artist cannot control (their pain tolerance, their post-care, how their skin healed, whether they later changed their aesthetic preferences). Public 1-star reviews based on these factors unfairly punish artists.
2. **The relationship is intimate and high-stakes.** Customers who had a difficult emotional experience may be reluctant to post honest negative reviews — they sat with the artist for hours and don't want public confrontation. This biases reviews toward dishonestly positive.
3. **Star anxiety damages the craft.** Artists who fear bad reviews stop taking risks on ambitious custom work. They steer customers toward "safe" designs that will heal predictably and look photogenic.
4. **Review systems are gameable.** Fake positive reviews from friends, fake negative reviews from competitors, and review-bombing campaigns all reliably degrade signal quality.

We have alternative mechanisms that cover the four functions of reviews:

- **Trust** is covered by tiered verification (RSSS health inspection on file, bloodborne pathogen cert verified by an admin).
- **Quality** is covered by the healed-photo portfolio uploaded by verified customers from real Needlr bookings — a stronger signal than text reviews because it's harder to fake and more directly relevant to the actual question ("does this artist's work hold up?").
- **Accountability** is covered by private structured feedback to Needlr and by the behavioral signals dashboard.
- **Fit** is the weakest of the four — partially covered by behavioral signals and the portfolio aesthetic, not by any equivalent to "Sarah was patient with my anxiety." We accept this gap.

## Decision

Needlr will not have public reviews of any kind. Specifically:

- No star ratings, thumbs up/down, or any other public quantified rating
- No public text reviews or testimonials
- No "X% would book again" or any other public aggregate of customer opinion
- No customer-attributed quotes anywhere on artist profiles
- No "verified review" badges (we have nothing to badge)

We will have:

- **Private structured feedback** (`BookingFeedback`) submitted by customers after completed bookings, visible only to Needlr admins. Drives an internal trust & safety dashboard. Never shown to artists or other customers.
- **Behavioral signals** on artist profiles, computed from platform data: response time, completion rate, healed photo upload rate, repeat client rate. Factual, computed, never opinion-based.
- **Implicit endorsement via healed-photo uploads.** A customer who uploads a healed photo at the 4-month prompt is implicitly endorsing the artist. The presence and quality of healed-work uploads serves as the de-facto quality review.

## Consequences

**Positive:**
- Artists can take creative risks without fearing a public 1-star review based on factors outside their control.
- Customers can give honest negative feedback (privately, to admins) without the social discomfort of publicly trashing someone they sat with for four hours.
- No review-bombing or fake-review attack surface.
- Discovery sort/filter logic stays clean — no "highest rated" sort to manipulate.

**Negative:**
- Customers conditioned to expect star ratings will perceive Needlr as missing a feature. This must be addressed in onboarding/marketing copy: "We use healed work and verified credentials instead of stars."
- The "fit" signal (warmth, communication style, vibe) is weaker than on review-based platforms. We compensate with the artist's bio, portfolio aesthetic, and behavioral signals like response time.
- New artists with few completed bookings have no behavioral signals to display. We accept this — they get the "Verified" tier as their initial trust signal, and time builds the rest.

## v2 consideration

Aggregate trust badges (Option D from the planning conversation) — earned recognition badges like "Reliably On Time" computed from the same private feedback data, surfaced publicly as positive signals only. Deferred to v2 once we have ≥6 months of feedback data to set non-arbitrary thresholds. Even then, badges are positive-only — we never publicly indicate that an artist *lacks* a badge because that becomes a de-facto negative review.

## Defending against future erosion

The arguments for adding reviews will mostly come from inside the building:

- "Customers keep asking for reviews" — they ask because they expect them, not because they're missing the underlying signal. Direct them to healed photos and verification.
- "Other platforms have them" — yes, and Instagram has likes. We've already decided that "other platforms have it" is not a sufficient argument.
- "Just internal stars, not text" — no. Star averages are public reviews in everything but name.
- "Just for the booking, not the artist" — no. Booking-level scores aggregate to artist-level scores in customers' minds and our admin tools.

The bar to overturn this ADR is the same as ADR-001: written proposal showing material harm to the booking metric attributable to the absence of the feature, and that the proposed addition is the smallest possible change.

## Related

- ADR-001: No social features
- `docs/FEATURE_SPECS.md` § Trust & Safety
- `docs/DOMAIN_MODEL.md` § BookingFeedback

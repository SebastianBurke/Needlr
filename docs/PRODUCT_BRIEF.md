# Needlr — Product Brief

## The problem

Tattoo discovery and booking today happens almost entirely on Instagram. This is a bad fit for both sides of the market:

**For customers**, Instagram is a discovery tool that doesn't actually support discovery. You can't filter by location, you can't filter by tattoo style, you can't see who's licensed, you can't see availability, you can't see healed work distinct from fresh work, and the algorithmic feed surfaces popular accounts instead of relevant ones. The booking process is a chaotic DM exchange that leaks across multiple apps (Instagram → email → Square → text).

**For artists**, Instagram is a content-treadmill that punishes the craft. Algorithmic reach means artists spend hours producing reels and chasing trends instead of tattooing. Likes, comments, and follower counts create parasocial pressure. Customers send fragmented DMs with incomplete briefs. There's no structured booking, no deposit handling, no calendar integration.

The shared root cause: Instagram is a social network being used as a professional marketplace. The social-network mechanics (likes, follows, comments, the feed, parasocial intimacy) are at odds with the actual job to be done (find a licensed local artist whose style fits what you want, and book a session with them).

## The product

Needlr is a location-first tattoo portfolio and booking platform that strips out the social-network mechanics and keeps only what serves the booking outcome.

The core loop:

1. A customer opens Needlr, sees a map of nearby licensed artists and studios.
2. They filter by style, verification status, and availability.
3. They view an artist's portfolio — fresh and healed work, organized by style and body placement, no likes, no comments, no follower counts.
4. They submit a structured booking request with references, description, body placement, size, and requested date.
5. The artist accepts, declines, or asks for more info.
6. On acceptance, the deposit is captured via Stripe Connect and a booking-scoped message thread opens for working out the details.
7. After the session, the artist marks it complete. Four months later, the customer is prompted to upload a healed photo, which becomes part of the artist's portfolio as verified healed work.

The ultimate metric is **completed bookings**. Not active users, not session time, not engagement. Bookings.

## Why this works

Several things line up to make Needlr structurally better than Instagram for this job:

- **Healed-work portfolio uploaded by verified customers** is a quality signal Instagram structurally cannot replicate. It's also the strongest possible accountability mechanism without resorting to public reviews.
- **Location-first discovery with PostGIS** makes the "near me" query genuinely fast and accurate, which Instagram cannot do at all.
- **Structured booking requests** replace the chaotic intro DM with a complete brief on first contact, which artists actively prefer.
- **No likes, no follows, no algorithmic feed** removes the content-treadmill pressure on artists and the parasocial pressure on customers.
- **Booking-scoped messaging** prevents Needlr from becoming yet another DM inbox while still solving the post-acceptance coordination problem.
- **Tiered verification anchored to local health-authority inspections** (Montréal: RSSS at launch; per-jurisdiction config for other markets) gives a real trust signal that's not available on any social platform.

## Who this is for

**Primary customers (the bookable):** Adults in or visiting Montréal who want a tattoo and currently use Instagram + word of mouth to find an artist. They are tired of the discovery process. They will pay a deposit to book.

**Primary artists (the supply):** Licensed tattoo artists working in Montréal — both shop-affiliated and solo/private studios. They are tired of Instagram's content demands and the chaos of DM-based booking. They are willing to maintain a portfolio on a second platform if it generates real bookings.

**Not the audience:**
- Casual tattoo browsers looking for inspiration (use Pinterest)
- Artists who want a follower count and brand presence (Instagram)
- Multi-shop chains with non-tattooing owners and complex revenue splits (need their own thing)
- Anyone outside Montréal at launch

## Anti-goals

- **We are not trying to maximize time-on-platform.** A user who finds an artist in 90 seconds, books, and closes the app is the success case.
- **We are not trying to be a social network.** ADR-001 is binding.
- **We are not trying to mediate the artist-customer relationship long-term.** Once a customer has a regular artist, they may book entirely off-platform. That's fine. New discovery and booking infrastructure is the value proposition.
- **We are not trying to be a payments company.** Stripe Connect handles money; we never touch funds.
- **We are not trying to expand prematurely.** Montréal-only at launch. Other cities only when Montréal is healthy.

## Launch scope

Option C from the planning conversation: full v1 as designed, including all features in `docs/FEATURE_SPECS.md`. Estimated 8-10 months of solo dev pace. The user has accepted this scope explicitly.

V2 and later are out of scope. See `docs/FEATURE_SPECS.md` for the explicit v2 list.

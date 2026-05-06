using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Needlr.Domain.Availability;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Identity;
using Needlr.Domain.Portfolio;
using Needlr.Domain.Studios;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only fixture seeder. Populates the dev DB with enough density
/// (customers, artists, studios, credentials, portfolios, availability, sample bookings)
/// to drive browser-based testing of the discovery map, profiles, search filters, and the
/// booking flow at meaningful concentration.
///
/// <para>
/// Idempotent via a sentinel-email check: if <c>artist.001@needlr.dev</c> already exists,
/// the seeder no-ops. To re-seed, drop the dev database (or specifically the
/// <c>users</c> + dependent tables) and restart the API.
/// </para>
///
/// <para>
/// Registered only by <see cref="DependencyInjection.AddNeedlrDevelopmentFixtures"/>;
/// production never sees this service. Runs after <see cref="DataSeeder"/> (which seeds
/// the Montréal jurisdiction, Admin role, and canonical TattooStyles) — those rows are
/// prerequisites for the fixtures.
/// </para>
///
/// <para>
/// Default credentials for every dev account: password <c>Devpass12345!</c>.
/// Email layout:
/// <list type="bullet">
///   <item><description><c>admin@needlr.dev</c> — Admin role</description></item>
///   <item><description><c>customer.NNN@needlr.dev</c> — Customer role, NNN = 001..030</description></item>
///   <item><description><c>artist.NNN@needlr.dev</c> — Artist role, NNN = 001..150</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class DevelopmentFixtureSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DevelopmentFixtureSeeder> logger) : IHostedService
{
    public const string DefaultPassword = "Devpass12345!";
    public const string AdminEmail = "admin@needlr.dev";
    public const string SentinelEmail = "artist.001@needlr.dev";

    private const int CustomerCount = 30;
    private const int ArtistCount = 150;
    private const int StudioCount = 50;
    private const int ProjectionDays = 90;
    private const int MinPortfolioPieces = 5;
    private const int MaxPortfolioPieces = 15;
    private const int RngSeed = 42;

    private static readonly GeometryFactory GeoFactory =
        new(new PrecisionModel(), srid: 4326);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<NeedlrDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (await db.Users.AnyAsync(u => u.Email == SentinelEmail, cancellationToken))
        {
            logger.LogInformation(
                "DevelopmentFixtureSeeder: sentinel '{Email}' exists, skipping.", SentinelEmail);
            return;
        }

        var styles = await db.TattooStyles
            .Where(s => s.IsCanonical)
            .ToListAsync(cancellationToken);
        if (styles.Count == 0)
        {
            logger.LogWarning(
                "DevelopmentFixtureSeeder: canonical tattoo styles missing — DataSeeder must run first.");
            return;
        }

        var jurisdictionId = DataSeeder.MontrealJurisdictionId;
        if (!await db.Jurisdictions.AnyAsync(j => j.Id == jurisdictionId, cancellationToken))
        {
            logger.LogWarning(
                "DevelopmentFixtureSeeder: Montréal jurisdiction missing — DataSeeder must run first.");
            return;
        }

        await EnsureRolesExistAsync(roleManager, ["Customer", "Artist"]);

        var rng = new Random(RngSeed);
        var sw = Stopwatch.StartNew();

        await EnsureAdminUserAsync(userManager, cancellationToken);

        var customers = await SeedCustomersAsync(db, userManager, rng, cancellationToken);
        logger.LogInformation("Dev seed: {N} customers ({Ms} ms total)", customers.Count, sw.ElapsedMilliseconds);

        var artists = await SeedArtistsAsync(db, userManager, styles, rng, cancellationToken);
        logger.LogInformation("Dev seed: {N} artists ({Ms} ms total)", artists.Count, sw.ElapsedMilliseconds);

        var (studios, affiliations) = await SeedStudiosAndAffiliationsAsync(db, artists, rng, cancellationToken);
        logger.LogInformation(
            "Dev seed: {S} studios + {A} affiliations ({Ms} ms total)",
            studios.Count, affiliations.Count, sw.ElapsedMilliseconds);

        await SeedCredentialsAsync(db, jurisdictionId, studios, artists, rng, cancellationToken);
        logger.LogInformation("Dev seed: credentials ({Ms} ms total)", sw.ElapsedMilliseconds);

        await SeedPortfolioAsync(db, artists, styles, rng, cancellationToken);
        logger.LogInformation("Dev seed: portfolios ({Ms} ms total)", sw.ElapsedMilliseconds);

        await SeedAvailabilityAsync(db, artists, cancellationToken);
        logger.LogInformation("Dev seed: availability ({Ms} ms total)", sw.ElapsedMilliseconds);

        await SeedSampleBookingsAsync(db, customers, artists, affiliations, rng, cancellationToken);
        logger.LogInformation("Dev seed: bookings ({Ms} ms total)", sw.ElapsedMilliseconds);

        sw.Stop();
        logger.LogInformation(
            "DevelopmentFixtureSeeder complete in {Ms} ms. Login with {Email} / {Pwd} (any seeded account uses the same password).",
            sw.ElapsedMilliseconds, SentinelEmail, DefaultPassword);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ─────────────────────────────────────────────────────────────────────────────
    // Roles + admin user
    // ─────────────────────────────────────────────────────────────────────────────

    private static async Task EnsureRolesExistAsync(
        RoleManager<IdentityRole<Guid>> roleManager, IEnumerable<string> roleNames)
    {
        foreach (var name in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(name) { Id = Guid.NewGuid() });
            }
        }
    }

    private async Task EnsureAdminUserAsync(
        UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(AdminEmail);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            Role = UserRole.Admin
        };
        var result = await userManager.CreateAsync(admin, DefaultPassword);
        ThrowIfFailed(result, $"create admin {AdminEmail}");
        await userManager.AddToRoleAsync(admin, DataSeeder.AdminRole);

        logger.LogInformation("Dev seed: admin user '{Email}' created.", AdminEmail);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Customers
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task<List<CustomerProfile>> SeedCustomersAsync(
        NeedlrDbContext db, UserManager<ApplicationUser> userManager,
        Random rng, CancellationToken cancellationToken)
    {
        var profiles = new List<CustomerProfile>(CustomerCount);

        for (int i = 1; i <= CustomerCount; i++)
        {
            var email = $"customer.{i:D3}@needlr.dev";
            var displayName = $"{FirstNames[(i - 1) % FirstNames.Length]} {LastNames[(i * 7) % LastNames.Length]}";

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Customer
            };
            var create = await userManager.CreateAsync(user, DefaultPassword);
            ThrowIfFailed(create, $"create customer {email}");
            await userManager.AddToRoleAsync(user, "Customer");

            Point? loc = null;
            if (rng.Next(0, 2) == 0)
            {
                var (lat, lng) = JitterAroundMontreal(rng, spread: 0.05);
                loc = GeoFactory.CreatePoint(new Coordinate(lng, lat));
            }

            var profile = new CustomerProfile(
                id: Guid.NewGuid(),
                userId: user.Id,
                displayName: displayName,
                preferredSearchRadiusKm: 5 + rng.Next(0, 30),
                location: loc);

            db.CustomerProfiles.Add(profile);
            profiles.Add(profile);
        }

        await db.SaveChangesAsync(cancellationToken);
        return profiles;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Artists
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task<List<Artist>> SeedArtistsAsync(
        NeedlrDbContext db, UserManager<ApplicationUser> userManager,
        IReadOnlyList<TattooStyle> styles, Random rng,
        CancellationToken cancellationToken)
    {
        var artists = new List<Artist>(ArtistCount);

        for (int i = 1; i <= ArtistCount; i++)
        {
            var email = $"artist.{i:D3}@needlr.dev";
            var displayName =
                $"{FirstNames[(i * 3) % FirstNames.Length]} {LastNames[(i * 11) % LastNames.Length]}";

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Artist
            };
            var create = await userManager.CreateAsync(user, DefaultPassword);
            ThrowIfFailed(create, $"create artist {email}");
            await userManager.AddToRoleAsync(user, "Artist");

            var styleSample = SampleStyles(styles, rng, count: 1 + rng.Next(0, 4));
            var primaryStyleName = styleSample[0].Name;

            var bio = string.Format(
                BioTemplates[i % BioTemplates.Length],
                primaryStyleName.ToLowerInvariant());

            var artist = new Artist(
                id: Guid.NewGuid(),
                userId: user.Id,
                displayName: displayName,
                bio: bio,
                yearsExperience: 1 + rng.Next(0, 25),
                hourlyRateCad: 100m + rng.Next(0, 41) * 5,    // 100..300 in $5 steps
                shopMinimumCad: 100m + rng.Next(0, 21) * 5,   // 100..200 in $5 steps
                cancellationPolicy: PickCancellationPolicy(rng));

            artist.AcceptingNewBookings = rng.Next(0, 5) > 0;     // ~80%
            artist.PaymentStatus = PickPaymentStatus(i);
            if (artist.PaymentStatus is ArtistPaymentStatus.Active or ArtistPaymentStatus.Restricted)
            {
                artist.StripeConnectAccountId = $"acct_devstub_seed_{i:D3}";
            }

            foreach (var style in styleSample)
            {
                artist.Styles.Add(style);
            }

            db.Artists.Add(artist);
            artists.Add(artist);

            // Save in batches to keep change-tracker memory bounded.
            if (i % 50 == 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return artists;
    }

    private static ArtistPaymentStatus PickPaymentStatus(int index) =>
        index switch
        {
            <= 140 => ArtistPaymentStatus.Active,
            <= 145 => ArtistPaymentStatus.OnboardingInProgress,
            <= 148 => ArtistPaymentStatus.NotOnboarded,
            _      => ArtistPaymentStatus.Restricted
        };

    private static CancellationPolicy PickCancellationPolicy(Random rng) =>
        rng.Next(0, 10) switch
        {
            < 2 => CancellationPolicy.Strict,
            < 8 => CancellationPolicy.Standard,
            _   => CancellationPolicy.Flexible
        };

    private static List<TattooStyle> SampleStyles(IReadOnlyList<TattooStyle> styles, Random rng, int count)
    {
        var pool = styles.ToList();
        var picked = new List<TattooStyle>(count);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            var idx = rng.Next(pool.Count);
            picked.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return picked;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Studios + affiliations (one pass — affiliations need the studio.CreatedByArtistId)
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task<(List<Studio> Studios, List<ArtistStudioAffiliation> Affiliations)>
        SeedStudiosAndAffiliationsAsync(
            NeedlrDbContext db, IReadOnlyList<Artist> artists, Random rng,
            CancellationToken cancellationToken)
    {
        // Plan: distribute artists across studios first (no entities yet) so the studio's
        // CreatedByArtistId matches its Founder affiliation. Each artist receives exactly
        // one primary affiliation.
        var queue = new Queue<Artist>(artists);
        var plans = new List<(StudioType Type, string Name, int Index, List<Artist> Members)>(StudioCount);

        for (int i = 0; i < StudioCount && queue.Count > 0; i++)
        {
            StudioType type = i switch
            {
                < 35 => StudioType.Shop,
                < 45 => StudioType.Solo,
                _    => StudioType.Private
            };

            int desired = type switch
            {
                StudioType.Solo    => 1,
                StudioType.Private => 2 + rng.Next(0, 2),
                StudioType.Shop    => 2 + rng.Next(0, 5),
                _                  => 1
            };

            var members = new List<Artist>(desired);
            for (int j = 0; j < desired && queue.Count > 0; j++)
            {
                members.Add(queue.Dequeue());
            }
            if (members.Count == 0) break;

            plans.Add((type, StudioNames[i], i, members));
        }

        // Distribute any leftover artists across Shop plans as additional members.
        var shopPlans = plans.Where(p => p.Type == StudioType.Shop).ToList();
        while (queue.Count > 0 && shopPlans.Count > 0)
        {
            var plan = shopPlans[rng.Next(shopPlans.Count)];
            plan.Members.Add(queue.Dequeue());
        }

        var studios = new List<Studio>(plans.Count);
        var affiliations = new List<ArtistStudioAffiliation>();

        foreach (var plan in plans)
        {
            var (neighborhood, baseLat, baseLng) = Neighborhoods[plan.Index % Neighborhoods.Length];
            var lat = baseLat + (rng.NextDouble() - 0.5) * 0.012;   // ~700m jitter
            var lng = baseLng + (rng.NextDouble() - 0.5) * 0.012;
            var location = GeoFactory.CreatePoint(new Coordinate(lng, lat));

            var streetNumber = 100 + rng.Next(0, 9900);
            var streetName = StreetNames[rng.Next(StreetNames.Length)];
            var address = $"{streetNumber} {streetName}, {neighborhood}, Montréal, QC";

            var founder = plan.Members[0];
            var studio = new Studio(
                id: Guid.NewGuid(),
                name: plan.Name,
                studioType: plan.Type,
                location: location,
                address: address,
                createdByArtistId: founder.Id,
                description: StudioDescriptions[plan.Index % StudioDescriptions.Length]);

            for (int day = 0; day < 7; day++)
            {
                var dow = (DayOfWeek)day;
                bool closed = dow is DayOfWeek.Sunday or DayOfWeek.Monday;
                studio.Hours.Add(new StudioHours(
                    id: Guid.NewGuid(),
                    studioId: studio.Id,
                    dayOfWeek: dow,
                    openTime: new TimeOnly(11, 0),
                    closeTime: new TimeOnly(19, 0),
                    isClosed: closed));
            }

            db.Studios.Add(studio);
            studios.Add(studio);

            for (int j = 0; j < plan.Members.Count; j++)
            {
                var artist = plan.Members[j];
                AffiliationRole role;
                if (j == 0) role = AffiliationRole.Founder;
                else if (plan.Type == StudioType.Shop && j == 1 && rng.Next(0, 3) == 0)
                    role = AffiliationRole.Admin;
                else role = AffiliationRole.Member;

                var aff = new ArtistStudioAffiliation(
                    id: Guid.NewGuid(),
                    artistId: artist.Id,
                    studioId: studio.Id,
                    role: role,
                    affiliationType: AffiliationType.Permanent,
                    startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                    status: AffiliationStatus.Active,
                    isPrimary: true);
                db.ArtistStudioAffiliations.Add(aff);
                affiliations.Add(aff);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return (studios, affiliations);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Credentials (StudioCredential + ArtistCredential)
    // Mostly Verified so the discovery filter has results; some DocumentsSubmitted for
    // testing the filter-off case; a sliver Unverified to never appear in discovery.
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task SeedCredentialsAsync(
        NeedlrDbContext db, Guid jurisdictionId,
        IReadOnlyList<Studio> studios, IReadOnlyList<Artist> artists,
        Random rng, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var verifiedAt = DateTime.UtcNow.AddMonths(-3);

        for (int i = 0; i < studios.Count; i++)
        {
            var studio = studios[i];
            var inspection = new StudioCredential(
                id: Guid.NewGuid(),
                studioId: studio.Id,
                jurisdictionId: jurisdictionId,
                credentialType: StudioCredentialType.HealthInspection,
                issuedDate: today.AddMonths(-6),
                expiryDate: today.AddMonths(6),
                documentUrl: $"https://placehold.co/600x800?text=Inspection+{i + 1:D2}");

            // ~75% Verified, ~20% DocumentsSubmitted, ~5% Unverified
            var roll = rng.Next(0, 100);
            if (roll < 75)
            {
                inspection.VerificationStatus = VerificationStatus.Verified;
                inspection.VerifiedAt = verifiedAt;
            }
            else if (roll < 95)
            {
                inspection.VerificationStatus = VerificationStatus.DocumentsSubmitted;
            }
            else
            {
                inspection.VerificationStatus = VerificationStatus.Unverified;
            }
            db.StudioCredentials.Add(inspection);

            // Optional municipal registration for Shop studios.
            if (studio.StudioType == StudioType.Shop && rng.Next(0, 3) == 0)
            {
                var muni = new StudioCredential(
                    id: Guid.NewGuid(),
                    studioId: studio.Id,
                    jurisdictionId: jurisdictionId,
                    credentialType: StudioCredentialType.MunicipalRegistration,
                    issuedDate: today.AddYears(-3),
                    expiryDate: today.AddYears(2),
                    documentUrl: $"https://placehold.co/600x800?text=Municipal+{i + 1:D2}");
                muni.VerificationStatus = VerificationStatus.Verified;
                muni.VerifiedAt = verifiedAt;
                db.StudioCredentials.Add(muni);
            }
        }

        for (int i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];
            var roll = rng.Next(0, 100);
            VerificationStatus statusForBoth = roll switch
            {
                < 75 => VerificationStatus.Verified,
                < 95 => VerificationStatus.DocumentsSubmitted,
                _    => VerificationStatus.Unverified
            };

            void AddArtistCredential(ArtistCredentialType type, int monthsUntilExpiry)
            {
                var cred = new ArtistCredential(
                    id: Guid.NewGuid(),
                    artistId: artist.Id,
                    jurisdictionId: jurisdictionId,
                    credentialType: type,
                    issuedDate: today.AddMonths(-6),
                    expiryDate: today.AddMonths(monthsUntilExpiry),
                    documentUrl: $"https://placehold.co/600x800?text={type}+{i + 1:D3}");
                cred.VerificationStatus = statusForBoth;
                if (statusForBoth == VerificationStatus.Verified) cred.VerifiedAt = verifiedAt;
                db.ArtistCredentials.Add(cred);
            }

            AddArtistCredential(ArtistCredentialType.BloodbornePathogenCertification, monthsUntilExpiry: 6);
            AddArtistCredential(ArtistCredentialType.FormationHygieneEtSalubrite, monthsUntilExpiry: 18);

            if (i % 50 == 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Portfolio pieces + session photos (~5..15 pieces per artist)
    // Image URLs use picsum.photos with a deterministic seed so layouts stay stable.
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task SeedPortfolioAsync(
        NeedlrDbContext db, IReadOnlyList<Artist> artists,
        IReadOnlyList<TattooStyle> styles, Random rng,
        CancellationToken cancellationToken)
    {
        var bodyPlacements = Enum.GetValues<BodyPlacement>();
        int saveCounter = 0;

        for (int i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];
            int pieceCount = MinPortfolioPieces + rng.Next(0, MaxPortfolioPieces - MinPortfolioPieces + 1);

            for (int p = 0; p < pieceCount; p++)
            {
                var pieceId = Guid.NewGuid();
                var year = DateTime.UtcNow.Year - rng.Next(0, 4);
                var placement = bodyPlacements[rng.Next(bodyPlacements.Length)];
                var createdAt = DateTime.UtcNow.AddDays(-rng.Next(0, 730));

                var piece = new PortfolioPiece(
                    id: pieceId,
                    artistId: artist.Id,
                    bodyPlacement: placement,
                    yearCompleted: year,
                    createdAt: DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
                    title: PortfolioTitles[(i * 31 + p) % PortfolioTitles.Length],
                    description: PortfolioDescriptions[(i + p) % PortfolioDescriptions.Length],
                    approximateSizeCm: 5 + rng.Next(0, 40),
                    estimatedSessionLengthHours: 1m + rng.Next(0, 8) * 0.5m);

                // Reuse the artist's primary styles so portfolio aligns with their declared specialties,
                // then occasionally add an extra to broaden their tagged surface.
                var pieceStyles = artist.Styles
                    .OrderBy(_ => rng.Next())
                    .Take(1 + rng.Next(0, 2))
                    .ToList();
                if (rng.Next(0, 5) == 0)
                {
                    var extra = styles[rng.Next(styles.Count)];
                    if (!pieceStyles.Any(s => s.Id == extra.Id)) pieceStyles.Add(extra);
                }
                foreach (var s in pieceStyles)
                {
                    piece.Styles.Add(s);
                }

                var fresh = new SessionPhoto(
                    id: Guid.NewGuid(),
                    portfolioPieceId: pieceId,
                    order: 0,
                    photoType: PhotoType.Fresh,
                    imageUrl: $"https://picsum.photos/seed/needlr-{artist.Id:N}-{p}/800/800",
                    uploadedByUserId: artist.UserId,
                    uploadedByRole: UploadedByRole.Artist,
                    uploadedAt: DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
                    linkedSessionDate: DateTime.SpecifyKind(createdAt.AddDays(-1), DateTimeKind.Utc));
                piece.Sessions.Add(fresh);

                // ~30% of pieces older than 4 months get a healed photo too.
                if (createdAt < DateTime.UtcNow.AddMonths(-4) && rng.Next(0, 10) < 3)
                {
                    var healed = new SessionPhoto(
                        id: Guid.NewGuid(),
                        portfolioPieceId: pieceId,
                        order: 1,
                        photoType: PhotoType.Healed,
                        imageUrl: $"https://picsum.photos/seed/needlr-h-{artist.Id:N}-{p}/800/800",
                        uploadedByUserId: artist.UserId,
                        uploadedByRole: UploadedByRole.Customer,
                        uploadedAt: DateTime.SpecifyKind(createdAt.AddMonths(4), DateTimeKind.Utc),
                        linkedSessionDate: DateTime.SpecifyKind(createdAt.AddDays(-1), DateTimeKind.Utc));
                    piece.Sessions.Add(healed);
                }

                db.PortfolioPieces.Add(piece);
                saveCounter++;
            }

            if (saveCounter > 200)
            {
                await db.SaveChangesAsync(cancellationToken);
                saveCounter = 0;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Availability: weekly pattern (Tue-Sat open) + 90-day projection.
    // Bypasses IAvailabilityProjector — the projection rows we write here are what
    // discovery's availability filter will read.
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task SeedAvailabilityAsync(
        NeedlrDbContext db, IReadOnlyList<Artist> artists, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowUtc = DateTime.UtcNow;
        int batched = 0;

        for (int i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];

            for (int day = 0; day < 7; day++)
            {
                var dow = (DayOfWeek)day;
                var status = dow is DayOfWeek.Sunday or DayOfWeek.Monday
                    ? AvailabilityStatus.Closed
                    : AvailabilityStatus.Available;
                var maxHours = status == AvailabilityStatus.Available ? 6m : (decimal?)null;

                db.AvailabilityPatterns.Add(new AvailabilityPattern(
                    id: Guid.NewGuid(),
                    artistId: artist.Id,
                    dayOfWeek: dow,
                    status: status,
                    effectiveFrom: today.AddYears(-1),
                    maxSessionHours: maxHours));
                batched++;
            }

            for (int d = 0; d < ProjectionDays; d++)
            {
                var date = today.AddDays(d);
                bool open = date.DayOfWeek is not (DayOfWeek.Sunday or DayOfWeek.Monday);
                db.ArtistAvailabilityProjections.Add(new ArtistAvailabilityProjection(
                    id: Guid.NewGuid(),
                    artistId: artist.Id,
                    date: date,
                    isBookable: open && artist.AcceptingNewBookings,
                    remainingSessionHours: open ? 6m : 0m,
                    recomputedAt: nowUtc));
                batched++;
            }

            if (batched > 1500)
            {
                await db.SaveChangesAsync(cancellationToken);
                batched = 0;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Sample bookings: a handful spread across states so the inbox + history pages
    // have content. No MessageThread auto-open here — that path runs through the
    // OpenMessageThreadOnDepositCapturedHandler in real flows; for fixtures the row
    // states are enough.
    // ─────────────────────────────────────────────────────────────────────────────

    private async Task SeedSampleBookingsAsync(
        NeedlrDbContext db,
        IReadOnlyList<CustomerProfile> customers,
        IReadOnlyList<Artist> artists,
        IReadOnlyList<ArtistStudioAffiliation> affiliations,
        Random rng,
        CancellationToken cancellationToken)
    {
        // Only artists who are bookable (Active, AcceptingNewBookings) and have a primary studio.
        var primaryByArtist = affiliations
            .Where(a => a.IsPrimary)
            .ToDictionary(a => a.ArtistId, a => a.StudioId);

        var bookableArtists = artists
            .Where(a => a.PaymentStatus == ArtistPaymentStatus.Active
                        && a.AcceptingNewBookings
                        && primaryByArtist.ContainsKey(a.Id))
            .ToList();
        if (bookableArtists.Count == 0 || customers.Count == 0) return;

        var bodyPlacements = Enum.GetValues<BodyPlacement>();
        var nowUtc = DateTime.UtcNow;

        // Compose ~20 bookings across statuses.
        var statuses = new (BookingStatus Status, int Count, int DayOffset)[]
        {
            (BookingStatus.Requested, 5, +14),
            (BookingStatus.Confirmed, 8, +21),
            (BookingStatus.Completed, 5, -45),
            (BookingStatus.Declined, 1, -7),
            (BookingStatus.Expired, 1, -14)
        };

        int customerIdx = 0;
        int artistIdx = 0;

        foreach (var (status, count, dayOffset) in statuses)
        {
            for (int n = 0; n < count; n++)
            {
                var customer = customers[customerIdx++ % customers.Count];
                var artist = bookableArtists[artistIdx++ % bookableArtists.Count];
                var studioId = primaryByArtist[artist.Id];
                var requestedAt = DateTime.SpecifyKind(nowUtc.AddDays(dayOffset - 7), DateTimeKind.Utc);
                var requestedDate = DateOnly.FromDateTime(nowUtc.AddDays(dayOffset));

                // bookings.customer_id FKs to users.id (the auth record), not customer_profiles.id —
                // mirrors RequestBookingCommandHandler.cs which sources customerId from ICurrentUser.UserId.
                var booking = new Booking(
                    id: Guid.NewGuid(),
                    customerId: customer.UserId,
                    artistId: artist.Id,
                    studioId: studioId,
                    bookingType: BookingType.TattooSession,
                    requestedAt: requestedAt,
                    requestedDate: requestedDate,
                    estimatedDurationHours: 2m + rng.Next(0, 5),
                    description: BookingDescriptions[rng.Next(BookingDescriptions.Length)],
                    bodyPlacement: bodyPlacements[rng.Next(bodyPlacements.Length)],
                    depositAmountCad: 100m,
                    cancellationPolicySnapshot: artist.CancellationPolicy,
                    approximateSizeCm: 8 + rng.Next(0, 25),
                    estimatedTotalCad: 200m + rng.Next(0, 41) * 25);

                booking.Status = status;
                booking.StripePaymentIntentId = $"pi_devstub_seed_{Guid.NewGuid():N}";

                switch (status)
                {
                    case BookingStatus.Confirmed:
                        booking.AcceptedAt = requestedAt.AddHours(2);
                        booking.DepositCapturedAt = requestedAt.AddHours(2).AddSeconds(5);
                        booking.ConfirmedSessionDate = requestedDate.ToDateTime(new TimeOnly(13, 0)).ToUniversalTime();
                        break;
                    case BookingStatus.Completed:
                        booking.AcceptedAt = requestedAt.AddHours(2);
                        booking.DepositCapturedAt = requestedAt.AddHours(2).AddSeconds(5);
                        booking.ConfirmedSessionDate = requestedDate.ToDateTime(new TimeOnly(13, 0)).ToUniversalTime();
                        booking.CompletedAt = booking.ConfirmedSessionDate.Value.AddHours(3);
                        break;
                    case BookingStatus.Declined:
                        booking.DeclineReason = DeclineReason.OutsideMyStyle;
                        booking.DeclineNote = "Outside the styles I take on. Try one of my colleagues.";
                        break;
                    case BookingStatus.Expired:
                        // No additional fields needed; status alone is enough.
                        break;
                }

                db.Bookings.Add(booking);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private static (double Lat, double Lng) JitterAroundMontreal(Random rng, double spread) =>
        (45.5019 + (rng.NextDouble() - 0.5) * spread,
         -73.5674 + (rng.NextDouble() - 0.5) * spread);

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"DevelopmentFixtureSeeder failed to {action}: " +
                string.Join("; ", result.Errors.Select(e => $"{e.Code}={e.Description}")));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Static data: Montréal neighborhoods (approximate centers), studio names,
    // street names, names, bios, portfolio descriptions, booking descriptions.
    // ─────────────────────────────────────────────────────────────────────────────

    private static readonly (string Name, double Lat, double Lng)[] Neighborhoods =
    [
        ("Plateau-Mont-Royal", 45.5219, -73.5800),
        ("Mile End",           45.5236, -73.5996),
        ("Vieux-Montréal",     45.5048, -73.5544),
        ("Outremont",          45.5183, -73.6089),
        ("Verdun",             45.4604, -73.5687),
        ("Rosemont",           45.5500, -73.5800),
        ("Hochelaga",          45.5471, -73.5424),
        ("NDG",                45.4736, -73.6147),
        ("Saint-Henri",        45.4828, -73.5836),
        ("Villeray",           45.5476, -73.6156)
    ];

    private static readonly string[] StudioNames =
    [
        "L'Encre Noire", "Atelier Sérigraphie", "Studio Boréal", "Maisonneuve Ink",
        "Plateau Tattoo Co.", "Mile End Body Arts", "Vieux-Port Tattoo", "Outremont Studio",
        "Rosemont Ink Lab", "Verdun Tattoo Bar", "Studio Anomalie", "Black Cat Tattoo",
        "La Boutique Noire", "Petit Sphinx Studio", "Sundown Tattoo Parlor", "Arctic Wolf Tattoo",
        "Iron & Ink", "The Fine Line", "Saint-Laurent Studio", "Belle Province Tattoo",
        "Notre-Dame Ink", "Mont-Royal Body Arts", "Cartier Tattoo Co.", "Sainte-Catherine Studio",
        "Boulevard Ink", "Rue Verte Tattoo", "Atelier Cinabre", "Studio Mansarde",
        "Loup Solitaire", "L'Œil Noir", "Le Renard Roux", "Salon de l'Aube",
        "Maison Mauve", "Studio Arrow", "Hand of Ink", "The Dotwork Society",
        "Linework Lab", "Color Theory Tattoo", "Single Needle Society", "House of Black",
        "Petit Lion Studio", "La Fleur Sauvage", "Studio Pluie", "Atelier Vague",
        "Northern Cross Tattoo", "Jardin d'Encre", "Studio Brûlé", "Carbone Ink",
        "Le Chat Noir Studio", "Studio Nocturne"
    ];

    private static readonly string[] StreetNames =
    [
        "Rue Saint-Laurent", "Rue Saint-Denis", "Avenue du Mont-Royal", "Boulevard Saint-Joseph",
        "Rue Notre-Dame", "Rue Sainte-Catherine", "Avenue Bernard", "Avenue Laurier",
        "Boulevard de Maisonneuve", "Rue Wellington", "Rue Beaubien", "Rue Rachel",
        "Rue Marie-Anne", "Avenue Papineau", "Rue Sherbrooke", "Avenue du Parc"
    ];

    private static readonly string[] FirstNames =
    [
        "Camille", "Émile", "Sophie", "Marc-André", "Luna", "Matteo", "Aaliyah", "Jin",
        "Olivier", "Sébastien", "Marie", "Antoine", "Charlotte", "Hugo", "Léa", "Nathan",
        "Zoé", "Théo", "Chloé", "Raphaël", "Maya", "Adam", "Fatima", "Jonas",
        "Anaïs", "Kai", "Inès", "Daniel", "Élise", "Victor", "Yasmine", "Liam",
        "Mira", "Mathis", "Léo", "Sara", "Étienne", "Maëlle", "Tristan", "Noor",
        "Gabriel", "Salomé", "Romain", "Iris", "Nikolai", "Aurélie", "Felix", "Samira", "Pascal", "Naomi"
    ];

    private static readonly string[] LastNames =
    [
        "Bélanger", "Tremblay", "Dubois", "Lavoie", "Rodriguez", "Rossi", "Khan", "Park",
        "Gagné", "Martin", "Roy", "Bouchard", "Côté", "Pelletier", "Lévesque", "Gauthier",
        "Morin", "Lefebvre", "Cloutier", "Fortin", "Dion", "Boucher", "Patel", "Nguyen",
        "Singh", "Cohen", "O'Brien", "Schultz", "Wagner", "Mercier", "Simard", "Beaulieu",
        "Bergeron", "Ouellet", "Fontaine", "Lacroix", "Caron", "Hamel", "Beaudoin", "Charron",
        "Renaud", "Demers", "Allard", "Bisson", "Cousineau", "Doyon", "Émond", "Forest", "Gélinas", "Hébert"
    ];

    private static readonly string[] BioTemplates =
    [
        "Specializing in {0} pieces. Custom designs only.",
        "Heavy {0} and ornamental sleeves.",
        "Working primarily in {0}, occasional one-shot flash.",
        "{0} with a focus on small, considered work.",
        "Traditional + {0}. Walk-ins by appointment only.",
        "Long-standing focus on {0}; collaborative consults welcome.",
        "{0} portraits and large-scale custom work.",
        "Dotwork backgrounds and {0} foregrounds, hand-drawn linework.",
        "Botanicals and {0} — soft palettes, healed work prioritized.",
        "{0} sleeves, full backs, and large-scale narrative work.",
        "Custom {0} on women, queer + BIPOC clients prioritized.",
        "{0} apprentice-trained; building toward a custom-only book."
    ];

    private static readonly string[] StudioDescriptions =
    [
        "Custom-only studio in a converted brick walkup. Strict appointment policy.",
        "Multi-artist shop with a dedicated walk-in chair on weekends.",
        "Small private studio focused on long-form custom narrative work.",
        "Friendly neighborhood shop. Walk-ins on Saturdays, appointments otherwise.",
        "Quiet space behind the print shop. Appointments only, by referral.",
        "Light-filled studio at street level — large windows, slow tempo.",
        "Co-op of independent artists; book directly with each.",
        "Established 2014. Three chairs, four resident artists, one guest spot.",
        "Solo studio in a renovated triplex. Coffee on the stove.",
        "Members-only collective. Visitors by appointment via existing clientele."
    ];

    private static readonly string[] PortfolioTitles =
    [
        "Untitled", "Two of Cups", "Petals & Stems", "North Star", "The Long Night",
        "First Light", "Verbena", "Concrete & Smoke", "Ouroboros", "Kintsugi",
        "Salt Wind", "Amber + Bone", "Quiet River", "Cloud Study", "Cypress",
        "Iron Garden", "After Hours", "Linework Study #4", "Three Birds", "Feast",
        "Snake & Wheat", "Lotus", "Old Code", "Ferry Crossing", "Sister Moon"
    ];

    private static readonly string[] PortfolioDescriptions =
    [
        "Single session, fineline.", "Three sessions, healed photos at four months.",
        "Custom illustration; reference and discussion via consult.",
        "Sleeve in progress, sessions ongoing.", "Walk-in flash, healed result.",
        "Cover-up over older script work.", "Companion piece to a forearm sleeve.",
        "Large-scale, two sessions, fully healed.", "Solid blackwork outlines, gray wash fills.",
        "Decorative ornamental piece, designed to wrap the joint cleanly.",
        "Botanical study, one of a series.", "First piece for this client; small scale to start."
    ];

    private static readonly string[] BookingDescriptions =
    [
        "I want a small fineline botanical piece on my inner forearm — references attached. Open on date if you have something within the next month.",
        "Custom blackwork sleeve start: looking to do a chest-to-shoulder panel, ornamental, with negative space. Happy to consult first.",
        "Color realism portrait of my dog, palm-sized, on my upper arm. Reference photo attached.",
        "Single-needle script across the collarbone, three lines. Specific font in the references.",
        "Coverup of an old name tattoo on my shoulder; want to roll it into a sun/moon piece. Happy to discuss directions.",
        "Touch-up on a piece you did 14 months ago — minor blowout on a fineline near the wrist.",
        "Large dotwork mandala, half-back, in a few long sessions. Specific symmetry I want to discuss.",
        "Anime-style geometric composite — two characters with a lattice background. Open to your interpretation.",
        "Geometric piece on the calf, ~15cm, blackwork. Want to combine triangles with botanicals — reference mood board attached."
    ];
}

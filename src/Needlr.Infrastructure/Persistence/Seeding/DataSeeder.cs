using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Needlr.Domain.Portfolio;
using Needlr.Domain.Verification;

namespace Needlr.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent startup seed: ensures the Montréal <see cref="Jurisdiction"/> row exists, the
/// <c>Admin</c> Identity role exists, and the 32 canonical <see cref="TattooStyle"/> rows
/// from FEATURE_SPECS.md exist. Runs once at host startup.
/// </summary>
internal sealed class DataSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DataSeeder> logger) : IHostedService
{
    public static readonly Guid MontrealJurisdictionId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public const string AdminRole = "Admin";

    /// <summary>The 32 canonical tattoo styles from FEATURE_SPECS.md § Tattoo style canonical seed list.</summary>
    private static readonly (string Name, string Slug)[] CanonicalStyles =
    [
        ("American Traditional",     "american-traditional"),
        ("Neo-Traditional",          "neo-traditional"),
        ("Japanese (Irezumi)",       "japanese-irezumi"),
        ("Blackwork",                "blackwork"),
        ("Dotwork",                  "dotwork"),
        ("Geometric",                "geometric"),
        ("Fineline",                 "fineline"),
        ("Single Needle",            "single-needle"),
        ("Microrealism",             "microrealism"),
        ("Realism",                  "realism"),
        ("Black & Grey Realism",     "black-and-grey-realism"),
        ("Color Realism",            "color-realism"),
        ("Portrait",                 "portrait"),
        ("Watercolor",               "watercolor"),
        ("Trash Polka",              "trash-polka"),
        ("Illustrative",             "illustrative"),
        ("New School",               "new-school"),
        ("Sketch",                   "sketch"),
        ("Linework",                 "linework"),
        ("Ornamental",               "ornamental"),
        ("Tribal",                   "tribal"),
        ("Polynesian",               "polynesian"),
        ("Chicano",                  "chicano"),
        ("Lettering / Script",       "lettering-script"),
        ("Religious / Spiritual",    "religious-spiritual"),
        ("Surrealism",               "surrealism"),
        ("Botanical",                "botanical"),
        ("Animal",                   "animal"),
        ("Anime / Manga",            "anime-manga"),
        ("Ignorant Style",           "ignorant-style"),
        ("Cybersigilism",            "cybersigilism"),
        ("Abstract",                 "abstract"),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        // Apply EF migrations first so the seed queries find their tables. Idempotent — a no-op
        // when the schema is already current. In production prefer running `dotnet ef database
        // update` from a deploy step; we keep it here for parity with dev/test boot.
        var db = sp.GetRequiredService<NeedlrDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await SeedJurisdictionAsync(sp, cancellationToken);
        await SeedAdminRoleAsync(sp, cancellationToken);
        await SeedTattooStylesAsync(sp, cancellationToken);

        logger.LogInformation("DataSeeder completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedJurisdictionAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var db = sp.GetRequiredService<NeedlrDbContext>();
        var exists = await db.Jurisdictions.AnyAsync(j => j.Id == MontrealJurisdictionId, cancellationToken);
        if (exists) return;

        // Montréal config per FEATURE_SPECS.md § Required credentials (Montréal):
        //   studio inspection: yes (RSSS health inspection)
        //   artist license:    no  (Quebec doesn't license individual tattoo artists)
        //   hygiene training:  yes (Formation hygiène et salubrité)
        //   bloodborne cert:   yes
        var montreal = new Jurisdiction(
            id: MontrealJurisdictionId,
            name: "Montréal, Quebec, Canada",
            country: "Canada",
            region: "Quebec",
            city: "Montréal",
            requiresStudioInspection: true,
            requiresArtistLicense: false,
            requiresArtistHygieneTraining: true,
            requiresBloodbornePathogenCert: true);

        db.Jurisdictions.Add(montreal);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded Montréal jurisdiction.");
    }

    private async Task SeedAdminRoleAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (await roleManager.RoleExistsAsync(AdminRole)) return;

        var role = new IdentityRole<Guid>(AdminRole) { Id = Guid.NewGuid() };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed Admin role: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        logger.LogInformation("Seeded Admin role.");
    }

    private async Task SeedTattooStylesAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var db = sp.GetRequiredService<NeedlrDbContext>();
        var existingSlugs = await db.TattooStyles
            .Select(s => s.Slug)
            .ToListAsync(cancellationToken);
        var existingSet = existingSlugs.ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var (name, slug) in CanonicalStyles)
        {
            if (existingSet.Contains(slug)) continue;
            db.TattooStyles.Add(new TattooStyle(Guid.NewGuid(), name, slug, isCanonical: true));
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} canonical tattoo styles.", added);
        }
    }
}

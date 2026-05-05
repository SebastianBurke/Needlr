using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Needlr.Application.Abstractions;
using Needlr.Domain.Availability;
using Needlr.Domain.Bookings;
using Needlr.Domain.Identity;
using Needlr.Domain.Messaging;
using Needlr.Domain.Portfolio;
using Needlr.Domain.Studios;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence;

/// <summary>
/// Application DbContext. Combines ASP.NET Core Identity tables (via
/// <see cref="IdentityDbContext{TUser,TRole,TKey}"/>) with the Needlr Domain entities.
/// Uses snake_case naming, stores enums as strings, declares the postgis extension for
/// spatial columns, and applies all <c>IEntityTypeConfiguration&lt;&gt;</c> from this assembly.
/// Also implements <see cref="IUnitOfWork"/> so the Application layer's TransactionBehavior
/// can persist via the abstraction without referencing the concrete EF context.
/// </summary>
public class NeedlrDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    public NeedlrDbContext(DbContextOptions<NeedlrDbContext> options) : base(options) { }

    // Identity & users
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<ArtistLeadTime> ArtistLeadTimes => Set<ArtistLeadTime>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Studios
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<StudioHours> StudioHours => Set<StudioHours>();
    public DbSet<ArtistStudioAffiliation> ArtistStudioAffiliations => Set<ArtistStudioAffiliation>();

    // Verification
    public DbSet<Jurisdiction> Jurisdictions => Set<Jurisdiction>();
    public DbSet<StudioCredential> StudioCredentials => Set<StudioCredential>();
    public DbSet<ArtistCredential> ArtistCredentials => Set<ArtistCredential>();

    // Portfolio
    public DbSet<TattooStyle> TattooStyles => Set<TattooStyle>();
    public DbSet<PortfolioPiece> PortfolioPieces => Set<PortfolioPiece>();
    public DbSet<SessionPhoto> SessionPhotos => Set<SessionPhoto>();

    // Bookings
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingAttachment> BookingAttachments => Set<BookingAttachment>();
    public DbSet<BookingFeedback> BookingFeedbacks => Set<BookingFeedback>();

    // Availability
    public DbSet<AvailabilityPattern> AvailabilityPatterns => Set<AvailabilityPattern>();
    public DbSet<AvailabilityOverride> AvailabilityOverrides => Set<AvailabilityOverride>();
    public DbSet<BookingWindow> BookingWindows => Set<BookingWindow>();
    public DbSet<ArtistAvailabilityProjection> ArtistAvailabilityProjections => Set<ArtistAvailabilityProjection>();

    // Messaging
    public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReport> MessageReports => Set<MessageReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Declare postgis as a database-level extension so the migration creates it.
        // Required because Studio.Location and CustomerProfile.Location are geometry columns.
        builder.HasPostgresExtension("postgis");

        // Identity table mapping (must run before our customizations apply).
        base.OnModelCreating(builder);

        // Rename Identity tables to clean names. Defaults are "AspNetUsers", "AspNetRoles", etc.;
        // combined with snake_case via EFCore.NamingConventions they would become "asp_net_users",
        // which reads poorly. Explicit renames here win over the convention.
        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        // Apply all IEntityTypeConfiguration<> in this assembly (Persistence/Configurations/*).
        builder.ApplyConfigurationsFromAssembly(typeof(NeedlrDbContext).Assembly);

        // Global conventions:
        //   - All enums are persisted as strings (DB readability; Postgres native enums would
        //     also work but require explicit registration per type — we trade a bit of storage
        //     for less ceremony).
        //   - Decimals default to (10, 2) precision — money fields are the bulk of decimals;
        //     anything wanting different precision sets it explicitly in its configuration.
        ApplyGlobalConventions(builder);
    }

    private static void ApplyGlobalConventions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties())
            {
                var clr = prop.ClrType;
                var underlying = Nullable.GetUnderlyingType(clr) ?? clr;

                if (underlying.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(underlying);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    prop.SetValueConverter(converter);
                }
                else if (underlying == typeof(decimal) && prop.GetPrecision() is null)
                {
                    prop.SetPrecision(10);
                    prop.SetScale(2);
                }
            }
        }
    }
}

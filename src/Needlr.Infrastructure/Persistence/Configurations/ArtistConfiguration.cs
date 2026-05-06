using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Identity;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(Artist.DisplayNameMaxLength);
        builder.Property(a => a.Bio)
            .IsRequired()
            .HasMaxLength(Artist.BioMaxLength);

        builder.Property(a => a.YearsExperience).IsRequired();
        builder.Property(a => a.AcceptingNewBookings).IsRequired();
        builder.Property(a => a.PaymentStatus).IsRequired().HasMaxLength(30);
        builder.Property(a => a.CancellationPolicy).IsRequired().HasMaxLength(20);

        builder.Property(a => a.StripeConnectAccountId).HasMaxLength(100);
        builder.Property(a => a.IcalToken).HasMaxLength(64);
        builder.HasIndex(a => a.IcalToken).IsUnique().HasFilter("ical_token IS NOT NULL");

        // 1:1 with ApplicationUser.
        builder.HasIndex(a => a.UserId).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Artist>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Discovery often filters on (AcceptingNewBookings, PaymentStatus).
        builder.HasIndex(a => new { a.AcceptingNewBookings, a.PaymentStatus });

        // 1:many to ArtistLeadTime; cascade with the artist.
        builder.HasMany(a => a.LeadTimes)
            .WithOne()
            .HasForeignKey(lt => lt.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:many to ArtistStudioAffiliation; cascade with the artist.
        builder.HasMany(a => a.Affiliations)
            .WithOne()
            .HasForeignKey(aff => aff.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many with TattooStyle (Artist.Styles).
        builder.HasMany(a => a.Styles)
            .WithMany()
            .UsingEntity("artist_styles");
    }
}

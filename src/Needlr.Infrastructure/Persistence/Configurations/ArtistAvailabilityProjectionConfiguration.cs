using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Availability;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ArtistAvailabilityProjectionConfiguration : IEntityTypeConfiguration<ArtistAvailabilityProjection>
{
    public void Configure(EntityTypeBuilder<ArtistAvailabilityProjection> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Date).IsRequired();
        builder.Property(p => p.IsBookable).IsRequired();
        builder.Property(p => p.RemainingSessionHours).IsRequired();
        builder.Property(p => p.RecomputedAt).IsRequired();

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(p => p.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per (artist, date); the discovery filter joins on this.
        builder.HasIndex(p => new { p.ArtistId, p.Date }).IsUnique();
        // Discovery filter: "any bookable artist on this date range".
        builder.HasIndex(p => new { p.Date, p.IsBookable });
    }
}

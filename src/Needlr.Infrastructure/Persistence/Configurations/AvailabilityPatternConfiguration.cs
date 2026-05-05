using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Availability;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityPatternConfiguration : IEntityTypeConfiguration<AvailabilityPattern>
{
    public void Configure(EntityTypeBuilder<AvailabilityPattern> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DayOfWeek).IsRequired();
        builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
        builder.Property(p => p.EffectiveFrom).IsRequired();

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(p => p.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Projector queries: "patterns for artist effective on date X".
        builder.HasIndex(p => new { p.ArtistId, p.EffectiveFrom });
    }
}

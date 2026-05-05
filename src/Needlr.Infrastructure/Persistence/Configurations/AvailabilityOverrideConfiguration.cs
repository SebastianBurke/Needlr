using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Availability;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityOverrideConfiguration : IEntityTypeConfiguration<AvailabilityOverride>
{
    public void Configure(EntityTypeBuilder<AvailabilityOverride> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Date).IsRequired();
        builder.Property(o => o.Status).IsRequired().HasMaxLength(20);
        builder.Property(o => o.Reason).HasMaxLength(AvailabilityOverride.ReasonMaxLength);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(o => o.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // One override per (artist, date).
        builder.HasIndex(o => new { o.ArtistId, o.Date }).IsUnique();
    }
}

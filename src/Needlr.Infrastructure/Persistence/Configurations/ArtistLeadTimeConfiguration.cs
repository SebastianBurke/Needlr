using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ArtistLeadTimeConfiguration : IEntityTypeConfiguration<ArtistLeadTime>
{
    public void Configure(EntityTypeBuilder<ArtistLeadTime> builder)
    {
        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.BookingType).IsRequired().HasMaxLength(30);
        builder.Property(lt => lt.MinimumDays).IsRequired();

        // One lead-time row per (artist, booking type).
        builder.HasIndex(lt => new { lt.ArtistId, lt.BookingType }).IsUnique();
    }
}

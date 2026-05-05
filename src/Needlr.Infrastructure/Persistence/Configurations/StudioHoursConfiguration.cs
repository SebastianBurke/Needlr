using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Studios;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class StudioHoursConfiguration : IEntityTypeConfiguration<StudioHours>
{
    public void Configure(EntityTypeBuilder<StudioHours> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.DayOfWeek).IsRequired();
        builder.Property(h => h.OpenTime).IsRequired();
        builder.Property(h => h.CloseTime).IsRequired();
        builder.Property(h => h.IsClosed).IsRequired();

        // One row per (studio, day-of-week).
        builder.HasIndex(h => new { h.StudioId, h.DayOfWeek }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Availability;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class BookingWindowConfiguration : IEntityTypeConfiguration<BookingWindow>
{
    public void Configure(EntityTypeBuilder<BookingWindow> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WindowOpensAt).IsRequired();
        builder.Property(w => w.WindowClosesAt).IsRequired();
        builder.Property(w => w.TargetRangeStart).IsRequired();
        builder.Property(w => w.TargetRangeEnd).IsRequired();

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(w => w.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Find currently-open windows for artist".
        builder.HasIndex(w => new { w.ArtistId, w.WindowOpensAt, w.WindowClosesAt });
    }
}

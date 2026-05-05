using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Bookings;
using Needlr.Domain.Identity;
using Needlr.Domain.Portfolio;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class PortfolioPieceConfiguration : IEntityTypeConfiguration<PortfolioPiece>
{
    public void Configure(EntityTypeBuilder<PortfolioPiece> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(PortfolioPiece.TitleMaxLength);
        builder.Property(p => p.Description).HasMaxLength(PortfolioPiece.DescriptionMaxLength);
        builder.Property(p => p.BodyPlacement).IsRequired().HasMaxLength(20);
        builder.Property(p => p.ProgressionStatus).IsRequired().HasMaxLength(30);
        builder.Property(p => p.YearCompleted).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        // FreeformTags as a primitive collection (EF Core 8+ supports List<string> directly).
        builder.PrimitiveCollection(p => p.FreeformTags)
            .ElementType(b => b.HasMaxLength(PortfolioPiece.FreeformTagMaxLength));

        // Cascade with the artist.
        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(p => p.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional 1:1 link back to a Booking (for pieces created from a Needlr booking).
        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(p => p.LinkedBookingId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-many with TattooStyle.
        builder.HasMany(p => p.Styles)
            .WithMany()
            .UsingEntity("portfolio_piece_styles");

        // Paginated portfolio queries: "this artist's pieces, newest first".
        builder.HasIndex(p => new { p.ArtistId, p.CreatedAt });
        builder.HasIndex(p => p.BodyPlacement);
    }
}

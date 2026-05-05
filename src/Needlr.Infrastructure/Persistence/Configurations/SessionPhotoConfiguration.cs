using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Portfolio;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class SessionPhotoConfiguration : IEntityTypeConfiguration<SessionPhoto>
{
    public void Configure(EntityTypeBuilder<SessionPhoto> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Order).IsRequired();
        builder.Property(p => p.PhotoType).IsRequired().HasMaxLength(20);
        builder.Property(p => p.UploadedByRole).IsRequired().HasMaxLength(20);
        builder.Property(p => p.UploadedAt).IsRequired();
        builder.Property(p => p.IsHidden).IsRequired();

        builder.Property(p => p.ImageUrl).HasMaxLength(2000);
        builder.Property(p => p.HiddenReason).HasMaxLength(SessionPhoto.HiddenReasonMaxLength);

        // Cascade with the parent piece.
        builder.HasOne<PortfolioPiece>()
            .WithMany(p => p.Sessions)
            .HasForeignKey(p => p.PortfolioPieceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on uploader: photos persist beyond user lifecycle for portfolio integrity.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ordered display per piece.
        builder.HasIndex(p => new { p.PortfolioPieceId, p.Order });
    }
}

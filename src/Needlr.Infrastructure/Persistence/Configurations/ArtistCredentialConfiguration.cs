using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Identity;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ArtistCredentialConfiguration : IEntityTypeConfiguration<ArtistCredential>
{
    public void Configure(EntityTypeBuilder<ArtistCredential> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CredentialType).IsRequired().HasMaxLength(40);
        builder.Property(c => c.VerificationStatus).IsRequired().HasMaxLength(30);
        builder.Property(c => c.IssuedDate).IsRequired();
        builder.Property(c => c.ExpiryDate).IsRequired();

        builder.Property(c => c.DocumentUrl).HasMaxLength(2000);
        builder.Property(c => c.RejectionReason).HasMaxLength(ArtistCredential.RejectionReasonMaxLength);

        // Cascade with the artist (deleting an artist deletes their credentials).
        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(c => c.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Jurisdiction>()
            .WithMany()
            .HasForeignKey(c => c.JurisdictionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.ExpiryDate);
        builder.HasIndex(c => new { c.ArtistId, c.CredentialType, c.VerificationStatus });
    }
}

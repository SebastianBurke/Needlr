using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class StudioCredentialConfiguration : IEntityTypeConfiguration<StudioCredential>
{
    public void Configure(EntityTypeBuilder<StudioCredential> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CredentialType).IsRequired().HasMaxLength(40);
        builder.Property(c => c.VerificationStatus).IsRequired().HasMaxLength(30);
        builder.Property(c => c.IssuedDate).IsRequired();
        builder.Property(c => c.ExpiryDate).IsRequired();

        builder.Property(c => c.DocumentUrl).HasMaxLength(2000);
        builder.Property(c => c.RejectionReason).HasMaxLength(StudioCredential.RejectionReasonMaxLength);

        // Jurisdiction FK; restrict — we never delete jurisdictions.
        builder.HasOne<Jurisdiction>()
            .WithMany()
            .HasForeignKey(c => c.JurisdictionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Admin who verified; set null if the admin user is removed (preserve audit minimally).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Nightly expiry-scan job filters by ExpiryDate; verification queue filters by status.
        builder.HasIndex(c => c.ExpiryDate);
        builder.HasIndex(c => new { c.StudioId, c.CredentialType, c.VerificationStatus });
    }
}

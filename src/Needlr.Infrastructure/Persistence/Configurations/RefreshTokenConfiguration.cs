using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.TokenHashLength);
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.IsRevoked).IsRequired();

        // Refresh paths look up by hash; this is the hot path.
        builder.HasIndex(r => r.TokenHash).IsUnique();

        // Sweep / per-user revocation queries.
        builder.HasIndex(r => new { r.UserId, r.IsRevoked, r.ExpiresAt });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

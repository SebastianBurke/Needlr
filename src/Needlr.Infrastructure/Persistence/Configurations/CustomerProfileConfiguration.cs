using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Identity;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(CustomerProfile.DisplayNameMaxLength);

        builder.Property(c => c.PreferredSearchRadiusKm).IsRequired();

        // Spatial column. Geographic point in WGS84 (lat/lng), EPSG:4326.
        builder.Property(c => c.Location).HasColumnType("geometry(Point, 4326)");
        builder.HasIndex(c => c.Location).HasMethod("gist");

        // 1:1 with ApplicationUser; cascade so deleting a user deletes their profile.
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<CustomerProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many with TattooStyle (CustomerProfile.PreferredStyles).
        builder.HasMany(c => c.PreferredStyles)
            .WithMany()
            .UsingEntity("customer_profile_preferred_styles");
    }
}

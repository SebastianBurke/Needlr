using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Studios;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(Studio.NameMaxLength);
        builder.Property(s => s.Address).IsRequired().HasMaxLength(Studio.AddressMaxLength);
        builder.Property(s => s.Description).HasMaxLength(Studio.DescriptionMaxLength);
        builder.Property(s => s.StudioType).IsRequired().HasMaxLength(20);
        builder.Property(s => s.JoinPolicy).IsRequired().HasMaxLength(20);

        // Spatial column. Geographic point in WGS84.
        builder.Property(s => s.Location)
            .IsRequired()
            .HasColumnType("geometry(Point, 4326)");
        builder.HasIndex(s => s.Location).HasMethod("gist");

        // Useful for "list of studios in this city by name" admin queries.
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.StudioType);

        // 1:many → StudioHours, ArtistStudioAffiliation, StudioCredential. Cascade with the studio.
        builder.HasMany(s => s.Hours)
            .WithOne()
            .HasForeignKey(h => h.StudioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Affiliations)
            .WithOne()
            .HasForeignKey(aff => aff.StudioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Credentials)
            .WithOne()
            .HasForeignKey(c => c.StudioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

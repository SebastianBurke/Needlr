using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Verification;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class JurisdictionConfiguration : IEntityTypeConfiguration<Jurisdiction>
{
    public void Configure(EntityTypeBuilder<Jurisdiction> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Name).IsRequired().HasMaxLength(Jurisdiction.NameMaxLength);
        builder.Property(j => j.Country).IsRequired().HasMaxLength(Jurisdiction.CountryMaxLength);
        builder.Property(j => j.Region).IsRequired().HasMaxLength(Jurisdiction.RegionMaxLength);
        builder.Property(j => j.City).IsRequired().HasMaxLength(Jurisdiction.CityMaxLength);

        builder.HasIndex(j => j.Name).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Portfolio;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class TattooStyleConfiguration : IEntityTypeConfiguration<TattooStyle>
{
    public void Configure(EntityTypeBuilder<TattooStyle> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(TattooStyle.NameMaxLength);
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(TattooStyle.SlugMaxLength);
        builder.Property(s => s.IsCanonical).IsRequired();

        builder.HasIndex(s => s.Slug).IsUnique();
        builder.HasIndex(s => s.Name).IsUnique();

        // Discovery filter typically restricts to canonical styles.
        builder.HasIndex(s => s.IsCanonical);
    }
}

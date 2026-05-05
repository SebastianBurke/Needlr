using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Studios;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ArtistStudioAffiliationConfiguration : IEntityTypeConfiguration<ArtistStudioAffiliation>
{
    public void Configure(EntityTypeBuilder<ArtistStudioAffiliation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Role).IsRequired().HasMaxLength(20);
        builder.Property(a => a.AffiliationType).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.IsPrimary).IsRequired();

        // Roster queries for a studio: filter by Status, often by IsPrimary.
        builder.HasIndex(a => new { a.StudioId, a.Status });

        // "Find an artist's primary affiliation" — exactly one Active+IsPrimary per artist.
        builder.HasIndex(a => new { a.ArtistId, a.IsPrimary, a.Status });
    }
}

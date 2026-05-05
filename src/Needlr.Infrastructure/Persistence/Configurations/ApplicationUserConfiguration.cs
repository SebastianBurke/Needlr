using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Table name is set on the base type in NeedlrDbContext.OnModelCreating ("users").
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.Role).IsRequired().HasMaxLength(20);

        // Useful for admin queries that filter users by role.
        builder.HasIndex(u => u.Role);
    }
}

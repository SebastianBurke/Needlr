using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Moderation;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class UserWarningConfiguration : IEntityTypeConfiguration<UserWarning>
{
    public void Configure(EntityTypeBuilder<UserWarning> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Reason).IsRequired().HasMaxLength(UserWarning.ReasonMaxLength);
        builder.Property(w => w.IssuedAt).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.IssuedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.UserId, w.IssuedAt });
    }
}

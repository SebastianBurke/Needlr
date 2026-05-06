using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Notifications;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Type).IsRequired().HasMaxLength(40);
        builder.Property(p => p.EmailEnabled).IsRequired();
        builder.Property(p => p.PushEnabled).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per (User, Type). The dispatcher's pref lookup hits this index.
        builder.HasIndex(p => new { p.UserId, p.Type }).IsUnique();
    }
}

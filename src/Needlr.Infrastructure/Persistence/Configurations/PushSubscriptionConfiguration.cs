using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Notifications;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Endpoint)
            .IsRequired().HasMaxLength(PushSubscription.EndpointMaxLength);
        builder.Property(s => s.P256dh)
            .IsRequired().HasMaxLength(PushSubscription.P256dhMaxLength);
        builder.Property(s => s.Auth)
            .IsRequired().HasMaxLength(PushSubscription.AuthMaxLength);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Endpoint is the dedup key — re-registering the same browser updates the existing
        // row rather than inserting another. Per-user uniqueness because Endpoint URLs are
        // browser-instance scoped.
        builder.HasIndex(s => new { s.UserId, s.Endpoint }).IsUnique();
    }
}

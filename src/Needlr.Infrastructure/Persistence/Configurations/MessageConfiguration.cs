using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Messaging;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Body).IsRequired().HasMaxLength(Message.BodyMaxLength);
        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.IsReportedFlag).IsRequired();

        // Restrict on sender — message text is retained indefinitely per ADR-003 § Retention.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Thread display.
        builder.HasIndex(m => new { m.ThreadId, m.SentAt });
        // Admin moderation queue surfaces reported messages.
        builder.HasIndex(m => m.IsReportedFlag);
    }
}

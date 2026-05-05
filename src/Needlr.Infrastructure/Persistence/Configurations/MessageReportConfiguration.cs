using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Messaging;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class MessageReportConfiguration : IEntityTypeConfiguration<MessageReport>
{
    public void Configure(EntityTypeBuilder<MessageReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(40);
        builder.Property(r => r.ReportedAt).IsRequired();
        builder.Property(r => r.Resolution).HasMaxLength(30);
        builder.Property(r => r.Note).HasMaxLength(MessageReport.NoteMaxLength);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unresolved-reports queue uses (Resolution IS NULL).
        builder.HasIndex(r => r.Resolution);
        builder.HasIndex(r => r.ReportedAt);
    }
}

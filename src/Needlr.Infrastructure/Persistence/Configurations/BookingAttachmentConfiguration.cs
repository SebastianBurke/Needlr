using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Bookings;
using Needlr.Domain.Messaging;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class BookingAttachmentConfiguration : IEntityTypeConfiguration<BookingAttachment>
{
    public void Configure(EntityTypeBuilder<BookingAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        // Url is nullable post-purge per ADR-003 § Retention.
        builder.Property(a => a.Url).HasMaxLength(2000);
        builder.Property(a => a.OriginalFilename)
            .IsRequired().HasMaxLength(BookingAttachment.OriginalFilenameMaxLength);
        builder.Property(a => a.MimeType)
            .IsRequired().HasMaxLength(BookingAttachment.MimeTypeMaxLength);
        builder.Property(a => a.SizeBytes).IsRequired();
        builder.Property(a => a.UploadedAt).IsRequired();

        // Two optional FKs — exactly one of these is set per row (constructor-enforced).
        builder.HasOne<Booking>()
            .WithMany(b => b.Attachments)
            .HasForeignKey(a => a.BookingId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne<Message>()
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lookups by either parent.
        builder.HasIndex(a => a.BookingId);
        builder.HasIndex(a => a.MessageId);
    }
}

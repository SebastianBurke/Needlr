using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Bookings;
using Needlr.Domain.Messaging;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class MessageThreadConfiguration : IEntityTypeConfiguration<MessageThread>
{
    public void Configure(EntityTypeBuilder<MessageThread> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.OpenedAt).IsRequired();
        builder.Property(t => t.Status).IsRequired().HasMaxLength(20);

        // 1:1 with Booking. Cascade with the booking record.
        builder.HasOne<Booking>()
            .WithOne(b => b.MessageThread)
            .HasForeignKey<MessageThread>(t => t.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => t.BookingId).IsUnique();

        builder.HasMany(t => t.Messages)
            .WithOne()
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lock job: scan Active threads whose related booking hit terminal state >= 90d ago.
        // We index Status to bound the scan.
        builder.HasIndex(t => t.Status);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Bookings;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class BookingFeedbackConfiguration : IEntityTypeConfiguration<BookingFeedback>
{
    public void Configure(EntityTypeBuilder<BookingFeedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.CommunicationRating).IsRequired();
        builder.Property(f => f.CleanlinessRating).IsRequired();
        builder.Property(f => f.RespectedDesignBriefRating).IsRequired();
        builder.Property(f => f.WouldBookAgain).IsRequired();
        builder.Property(f => f.SubmittedAt).IsRequired();

        builder.Property(f => f.FreeText).HasMaxLength(BookingFeedback.FreeTextMaxLength);

        // 1:1 with Booking. Cascade delete with the booking — but in practice bookings aren't
        // deleted; this only matters in tests / extreme admin actions.
        builder.HasOne<Booking>(/* no nav */)
            .WithOne(b => b.Feedback)
            .HasForeignKey<BookingFeedback>(f => f.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(f => f.BookingId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Bookings;
using Needlr.Domain.Identity;
using Needlr.Domain.Studios;
using Needlr.Infrastructure.Identity;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingType).IsRequired().HasMaxLength(30);
        builder.Property(b => b.Status).IsRequired().HasMaxLength(30);
        builder.Property(b => b.RequestedAt).IsRequired();
        builder.Property(b => b.RequestedDate).IsRequired();
        builder.Property(b => b.EstimatedDurationHours).IsRequired();
        builder.Property(b => b.Description).IsRequired().HasMaxLength(Booking.DescriptionMaxLength);
        builder.Property(b => b.BodyPlacement).IsRequired().HasMaxLength(20);
        builder.Property(b => b.DepositAmountCad).IsRequired();
        builder.Property(b => b.CancellationPolicySnapshot).IsRequired().HasMaxLength(20);
        builder.Property(b => b.IsAttachmentsPurged).IsRequired();

        builder.Property(b => b.StripePaymentIntentId).HasMaxLength(200);
        builder.Property(b => b.DeclineReason).HasMaxLength(40);
        builder.Property(b => b.DeclineNote).HasMaxLength(Booking.DeclineNoteMaxLength);

        // FKs to user (customer) and artist + studio. Restrict on user/artist/studio because
        // bookings are records of work and should not vanish silently.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(b => b.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Studio>()
            .WithMany()
            .HasForeignKey(b => b.StudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Artist's inbox: filter by status, sort by RequestedAt.
        builder.HasIndex(b => new { b.ArtistId, b.Status });
        // Customer's bookings list.
        builder.HasIndex(b => new { b.CustomerId, b.Status });
        // Expiry job: scan Requested bookings older than 7 days.
        builder.HasIndex(b => new { b.Status, b.RequestedAt });
        // Reminder job: find Confirmed bookings with sessions ~24h out.
        builder.HasIndex(b => new { b.Status, b.ConfirmedSessionDate });
        // Attachment-purge job needs to find terminal-state bookings >1y old that aren't yet purged.
        builder.HasIndex(b => new { b.IsAttachmentsPurged, b.CompletedAt });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Needlr.Domain.Stripe;

namespace Needlr.Infrastructure.Persistence.Configurations;

internal sealed class StripeProcessedEventConfiguration : IEntityTypeConfiguration<StripeProcessedEvent>
{
    public void Configure(EntityTypeBuilder<StripeProcessedEvent> builder)
    {
        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(StripeProcessedEvent.EventIdMaxLength);
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(StripeProcessedEvent.EventTypeMaxLength);
        builder.Property(e => e.ProcessedAt).IsRequired();

        // Ops query: "what did we process recently?".
        builder.HasIndex(e => e.ProcessedAt);
    }
}

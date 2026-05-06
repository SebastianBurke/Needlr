namespace Needlr.Domain.Stripe;

/// <summary>
/// Records a Stripe webhook event id we've already processed so redeliveries land as
/// no-ops (per ADR's webhook-idempotency requirement). The event id is the natural key;
/// <see cref="ProcessedAt"/> is informational for ops triage.
/// </summary>
public sealed class StripeProcessedEvent
{
    public const int EventIdMaxLength = 100;
    public const int EventTypeMaxLength = 100;

    public string EventId { get; init; } = null!;
    public string EventType { get; init; } = null!;
    public DateTime ProcessedAt { get; init; }

    private StripeProcessedEvent() { }

    public StripeProcessedEvent(string eventId, string eventType, DateTime processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (eventId.Length > EventIdMaxLength)
            throw new ArgumentException($"EventId must be <= {EventIdMaxLength} chars.", nameof(eventId));
        if (eventType.Length > EventTypeMaxLength)
            throw new ArgumentException($"EventType must be <= {EventTypeMaxLength} chars.", nameof(eventType));
        if (processedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ProcessedAt must be UTC.", nameof(processedAt));

        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }
}

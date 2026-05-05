using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;

namespace Needlr.Domain.Bookings;

/// <summary>
/// A customer's booking with a specific artist. The state machine and refund logic are
/// documented in DOMAIN_MODEL.md § BookingStatus and FEATURE_SPECS.md § Bookings.
/// </summary>
public sealed class Booking
{
    public const int DescriptionMaxLength = 5000;
    public const int DeclineNoteMaxLength = 1000;
    public const decimal MinDepositAmountCad = 1m;
    public const decimal MaxEstimatedDurationHours = 24m;

    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ArtistId { get; init; }
    public Guid StudioId { get; init; }
    public BookingType BookingType { get; init; }
    public BookingStatus Status { get; set; } = BookingStatus.Requested;
    public DateTime RequestedAt { get; init; }
    public DateOnly RequestedDate { get; set; }
    public decimal EstimatedDurationHours { get; set; }
    public string Description { get; set; } = null!;
    public BodyPlacement BodyPlacement { get; set; }
    public int? ApproximateSizeCm { get; set; }

    public decimal? EstimatedTotalCad { get; set; }
    public decimal DepositAmountCad { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime? DepositCapturedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ConfirmedSessionDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Frozen at booking time so subsequent policy changes by the artist don't affect this booking.
    /// </summary>
    public CancellationPolicy CancellationPolicySnapshot { get; init; }

    public DeclineReason? DeclineReason { get; set; }
    public string? DeclineNote { get; set; }

    public bool IsAttachmentsPurged { get; set; }

    public ICollection<BookingAttachment> Attachments { get; set; } = new List<BookingAttachment>();
    public MessageThread? MessageThread { get; set; }
    public BookingFeedback? Feedback { get; set; }

    private Booking() { }

    public Booking(
        Guid id,
        Guid customerId,
        Guid artistId,
        Guid studioId,
        BookingType bookingType,
        DateTime requestedAt,
        DateOnly requestedDate,
        decimal estimatedDurationHours,
        string description,
        BodyPlacement bodyPlacement,
        decimal depositAmountCad,
        CancellationPolicy cancellationPolicySnapshot,
        int? approximateSizeCm = null,
        decimal? estimatedTotalCad = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (studioId == Guid.Empty) throw new ArgumentException("StudioId is required.", nameof(studioId));
        if (requestedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("RequestedAt must be UTC.", nameof(requestedAt));

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (description.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description must be <= {DescriptionMaxLength} chars.", nameof(description));

        if (estimatedDurationHours <= 0 || estimatedDurationHours > MaxEstimatedDurationHours)
            throw new ArgumentOutOfRangeException(nameof(estimatedDurationHours),
                $"Must be in (0, {MaxEstimatedDurationHours}].");

        if (depositAmountCad < MinDepositAmountCad)
            throw new ArgumentOutOfRangeException(nameof(depositAmountCad),
                $"Must be at least {MinDepositAmountCad} CAD.");

        if (approximateSizeCm is < 0)
            throw new ArgumentOutOfRangeException(nameof(approximateSizeCm), "Must be non-negative.");
        if (estimatedTotalCad is < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedTotalCad), "Must be non-negative.");

        Id = id;
        CustomerId = customerId;
        ArtistId = artistId;
        StudioId = studioId;
        BookingType = bookingType;
        RequestedAt = requestedAt;
        RequestedDate = requestedDate;
        EstimatedDurationHours = estimatedDurationHours;
        Description = description;
        BodyPlacement = bodyPlacement;
        ApproximateSizeCm = approximateSizeCm;
        DepositAmountCad = depositAmountCad;
        EstimatedTotalCad = estimatedTotalCad;
        CancellationPolicySnapshot = cancellationPolicySnapshot;
    }
}

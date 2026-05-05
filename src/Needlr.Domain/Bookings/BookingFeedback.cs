namespace Needlr.Domain.Bookings;

/// <summary>
/// Customer-to-Needlr private feedback after a Completed booking. Per ADR-002 this is never shown
/// to the artist or any other user — it drives the admin trust &amp; safety dashboard only.
/// </summary>
public sealed class BookingFeedback
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int FreeTextMaxLength = 2000;

    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public Guid CustomerId { get; init; }
    public int CommunicationRating { get; set; }
    public int CleanlinessRating { get; set; }
    public int RespectedDesignBriefRating { get; set; }
    public bool WouldBookAgain { get; set; }
    public string? FreeText { get; set; }
    public DateTime SubmittedAt { get; init; }

    private BookingFeedback() { }

    public BookingFeedback(
        Guid id,
        Guid bookingId,
        Guid customerId,
        int communicationRating,
        int cleanlinessRating,
        int respectedDesignBriefRating,
        bool wouldBookAgain,
        DateTime submittedAt,
        string? freeText = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId is required.", nameof(bookingId));
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));

        EnsureRatingInRange(communicationRating, nameof(communicationRating));
        EnsureRatingInRange(cleanlinessRating, nameof(cleanlinessRating));
        EnsureRatingInRange(respectedDesignBriefRating, nameof(respectedDesignBriefRating));

        if (freeText is { Length: > FreeTextMaxLength })
            throw new ArgumentException($"FreeText must be <= {FreeTextMaxLength} chars.", nameof(freeText));
        if (submittedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("SubmittedAt must be UTC.", nameof(submittedAt));

        Id = id;
        BookingId = bookingId;
        CustomerId = customerId;
        CommunicationRating = communicationRating;
        CleanlinessRating = cleanlinessRating;
        RespectedDesignBriefRating = respectedDesignBriefRating;
        WouldBookAgain = wouldBookAgain;
        FreeText = freeText?.Trim();
        SubmittedAt = submittedAt;
    }

    private static void EnsureRatingInRange(int value, string paramName)
    {
        if (value is < MinRating or > MaxRating)
            throw new ArgumentOutOfRangeException(paramName, $"Must be in [{MinRating}, {MaxRating}].");
    }
}

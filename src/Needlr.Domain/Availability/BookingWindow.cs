namespace Needlr.Domain.Availability;

/// <summary>
/// Optional batched-booking overlay. If any windows exist for an artist, requests are only
/// accepted during open windows for sessions within the window's target range.
/// </summary>
public sealed class BookingWindow
{
    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public DateTime WindowOpensAt { get; set; }
    public DateTime WindowClosesAt { get; set; }
    public DateOnly TargetRangeStart { get; set; }
    public DateOnly TargetRangeEnd { get; set; }

    private BookingWindow() { }

    public BookingWindow(
        Guid id,
        Guid artistId,
        DateTime windowOpensAt,
        DateTime windowClosesAt,
        DateOnly targetRangeStart,
        DateOnly targetRangeEnd)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (windowOpensAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("WindowOpensAt must be UTC.", nameof(windowOpensAt));
        if (windowClosesAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("WindowClosesAt must be UTC.", nameof(windowClosesAt));
        if (windowClosesAt <= windowOpensAt)
            throw new ArgumentException("WindowClosesAt must be after WindowOpensAt.", nameof(windowClosesAt));
        if (targetRangeEnd < targetRangeStart)
            throw new ArgumentException("TargetRangeEnd cannot be before TargetRangeStart.", nameof(targetRangeEnd));

        Id = id;
        ArtistId = artistId;
        WindowOpensAt = windowOpensAt;
        WindowClosesAt = windowClosesAt;
        TargetRangeStart = targetRangeStart;
        TargetRangeEnd = targetRangeEnd;
    }
}

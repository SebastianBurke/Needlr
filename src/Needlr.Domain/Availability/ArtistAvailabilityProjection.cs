namespace Needlr.Domain.Availability;

/// <summary>
/// Denormalized per-artist-per-day availability record, the only source the discovery
/// availability filter consults. Rebuilt nightly by Hangfire for a rolling 90-day window
/// and on-demand whenever the underlying availability data changes.
/// </summary>
public sealed class ArtistAvailabilityProjection
{
    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsBookable { get; set; }
    public decimal RemainingSessionHours { get; set; }
    public DateTime RecomputedAt { get; set; }

    private ArtistAvailabilityProjection() { }

    public ArtistAvailabilityProjection(
        Guid id,
        Guid artistId,
        DateOnly date,
        bool isBookable,
        decimal remainingSessionHours,
        DateTime recomputedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (remainingSessionHours < 0)
            throw new ArgumentOutOfRangeException(nameof(remainingSessionHours), "Must be non-negative.");
        if (recomputedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("RecomputedAt must be UTC.", nameof(recomputedAt));

        Id = id;
        ArtistId = artistId;
        Date = date;
        IsBookable = isBookable;
        RemainingSessionHours = remainingSessionHours;
        RecomputedAt = recomputedAt;
    }
}

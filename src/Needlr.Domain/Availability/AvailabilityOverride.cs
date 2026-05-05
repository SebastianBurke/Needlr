using Needlr.Domain.Enums;

namespace Needlr.Domain.Availability;

/// <summary>
/// One-off availability exception for a specific date. Overrides the recurring pattern.
/// </summary>
public sealed class AvailabilityOverride
{
    public const decimal MaxSessionHoursMax = 24m;
    public const int ReasonMaxLength = 500;

    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public DateOnly Date { get; init; }
    public AvailabilityStatus Status { get; set; }
    public decimal? MaxSessionHours { get; set; }

    /// <summary>Internal-only reason; not surfaced to customers.</summary>
    public string? Reason { get; set; }

    private AvailabilityOverride() { }

    public AvailabilityOverride(
        Guid id,
        Guid artistId,
        DateOnly date,
        AvailabilityStatus status,
        decimal? maxSessionHours = null,
        string? reason = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (maxSessionHours is { } hours && (hours <= 0 || hours > MaxSessionHoursMax))
            throw new ArgumentOutOfRangeException(nameof(maxSessionHours),
                $"Must be in (0, {MaxSessionHoursMax}] when set.");
        if (reason is { Length: > ReasonMaxLength })
            throw new ArgumentException($"Reason must be <= {ReasonMaxLength} chars.", nameof(reason));

        Id = id;
        ArtistId = artistId;
        Date = date;
        Status = status;
        MaxSessionHours = maxSessionHours;
        Reason = reason?.Trim();
    }
}

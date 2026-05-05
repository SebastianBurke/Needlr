using Needlr.Domain.Enums;

namespace Needlr.Domain.Availability;

/// <summary>
/// Recurring weekly availability for an artist. Multiple patterns can coexist with different
/// effective windows; the projector applies them along with overrides and bookings.
/// </summary>
public sealed class AvailabilityPattern
{
    public const decimal MaxSessionHoursMax = 24m;

    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public AvailabilityStatus Status { get; set; }
    public decimal? MaxSessionHours { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveUntil { get; set; }

    private AvailabilityPattern() { }

    public AvailabilityPattern(
        Guid id,
        Guid artistId,
        DayOfWeek dayOfWeek,
        AvailabilityStatus status,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil = null,
        decimal? maxSessionHours = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (effectiveUntil is { } until && until < effectiveFrom)
            throw new ArgumentException("EffectiveUntil cannot be before EffectiveFrom.", nameof(effectiveUntil));
        if (maxSessionHours is { } hours && (hours <= 0 || hours > MaxSessionHoursMax))
            throw new ArgumentOutOfRangeException(nameof(maxSessionHours),
                $"Must be in (0, {MaxSessionHoursMax}] when set.");

        Id = id;
        ArtistId = artistId;
        DayOfWeek = dayOfWeek;
        Status = status;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        MaxSessionHours = maxSessionHours;
    }
}

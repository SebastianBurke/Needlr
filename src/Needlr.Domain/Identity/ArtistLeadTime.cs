using Needlr.Domain.Enums;

namespace Needlr.Domain.Identity;

/// <summary>
/// Per-BookingType minimum lead time for an artist. Booking requests are rejected at form-submit
/// time if the requested date is sooner than today + MinimumDays.
/// </summary>
public sealed class ArtistLeadTime
{
    public const int MaxMinimumDays = 365;

    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public BookingType BookingType { get; init; }
    public int MinimumDays { get; set; }

    private ArtistLeadTime() { }

    public ArtistLeadTime(Guid id, Guid artistId, BookingType bookingType, int minimumDays)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (minimumDays is < 0 or > MaxMinimumDays)
            throw new ArgumentOutOfRangeException(nameof(minimumDays),
                $"Must be in [0, {MaxMinimumDays}].");

        Id = id;
        ArtistId = artistId;
        BookingType = bookingType;
        MinimumDays = minimumDays;
    }
}

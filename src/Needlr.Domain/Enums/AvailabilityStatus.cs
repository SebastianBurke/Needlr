namespace Needlr.Domain.Enums;

/// <summary>
/// Per-day availability status for an artist, used by AvailabilityPattern, AvailabilityOverride,
/// and the discovery availability filter.
/// </summary>
public enum AvailabilityStatus
{
    Available,

    /// <summary>Some capacity available, but constrained — typically with a MaxSessionHours cap.</summary>
    Limited,

    Closed
}

namespace Needlr.Domain.Enums;

/// <summary>
/// Type of booking. All three exist in the schema; only TattooSession is enabled in the v1 UI
/// per FEATURE_SPECS.md § BookingType scaffolding.
/// </summary>
public enum BookingType
{
    Consultation,
    TattooSession,
    Touchup
}

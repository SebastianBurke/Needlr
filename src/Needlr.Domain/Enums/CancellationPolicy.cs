namespace Needlr.Domain.Enums;

/// <summary>
/// The artist's deposit-refund policy. Snapshotted onto the Booking at request time so policy
/// changes don't affect existing bookings. Refund rules per FEATURE_SPECS.md § Deposit handling.
/// </summary>
public enum CancellationPolicy
{
    /// <summary>Deposit non-refundable on any customer cancellation; full refund only on artist cancellation.</summary>
    Strict,

    /// <summary>Full refund if customer cancels >7 days before; non-refundable inside 7 days.</summary>
    Standard,

    /// <summary>Full refund if customer cancels >48 hours before; non-refundable inside 48 hours.</summary>
    Flexible
}

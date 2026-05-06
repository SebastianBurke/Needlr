using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings;

/// <summary>
/// Encapsulates FEATURE_SPECS.md § Deposit handling. The refund decision depends on the
/// booking's <c>CancellationPolicySnapshot</c> and the time-of-cancel relative to the
/// confirmed session date. Phase 10 records the decision; Phase 11 actually issues the
/// Stripe refund based on this output.
/// </summary>
public static class CancellationRefundPolicy
{
    /// <summary>
    /// Computes the refund amount when the customer cancels a booking pre-session.
    /// </summary>
    public static decimal CustomerCancellationRefund(
        CancellationPolicy policy,
        decimal depositAmountCad,
        DateTime? confirmedSessionDateUtc,
        DateTime nowUtc)
    {
        // No confirmed date yet (e.g., cancelling a Requested booking): full refund — the
        // artist hasn't committed time and the deposit was only pre-authorized.
        if (confirmedSessionDateUtc is not { } session)
            return depositAmountCad;

        var hoursOut = (session - nowUtc).TotalHours;

        return policy switch
        {
            CancellationPolicy.Strict => 0m,
            CancellationPolicy.Standard => hoursOut > 24 * 7 ? depositAmountCad : 0m,
            CancellationPolicy.Flexible => hoursOut > 48 ? depositAmountCad : 0m,
            _ => 0m
        };
    }

    /// <summary>
    /// Artist-side cancellation: full refund regardless of policy or timing.
    /// </summary>
    public static decimal ArtistCancellationRefund(decimal depositAmountCad) => depositAmountCad;
}

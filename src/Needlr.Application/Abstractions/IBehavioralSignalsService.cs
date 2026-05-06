namespace Needlr.Application.Abstractions;

/// <summary>
/// Computes the public-facing behavioral signals for an artist (FEATURE_SPECS.md §
/// Behavioral signals). Each metric has a minimum-sample gate; below the gate the field
/// returns null so the FE can suppress display rather than show misleading low-N stats.
/// </summary>
public interface IBehavioralSignalsService
{
    Task<BehavioralSignals> ComputeAsync(Guid artistId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Per FEATURE_SPECS:
/// - <see cref="ResponseMedianHours"/>: median time from request to first action
///   (Accept/Decline/RequestInfo) over last 30 days. No min-N gate.
/// - <see cref="CompletionRatePercent"/>: % of confirmed bookings that reached Completed
///   over last 90 days. Only set when ≥10 bookings in window.
/// - <see cref="HealedPhotoRatePercent"/>: % of completed bookings (≥4 months old) where
///   the customer uploaded a healed photo. Only set when ≥10 eligible bookings.
/// - <see cref="RepeatClientRatePercent"/>: % of customers who booked a second session
///   within 12 months. Only set when ≥20 unique customers.
/// </summary>
public sealed record BehavioralSignals(
    double? ResponseMedianHours,
    double? CompletionRatePercent,
    double? HealedPhotoRatePercent,
    double? RepeatClientRatePercent);

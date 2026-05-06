using Needlr.Application.TrustSafety.GetTrustSafetyDashboard;

namespace Needlr.Application.Abstractions;

/// <summary>
/// Builds the admin trust &amp; safety dashboard. Implementation lives in Infrastructure
/// because the query joins across BookingFeedback + Booking + Artist with thresholds and
/// a small fixed safety-keyword list — easier to express in EF directly than via a
/// repository per shape.
/// </summary>
public interface ITrustSafetyDashboardService
{
    Task<TrustSafetyDashboardDto> GetAsync(CancellationToken cancellationToken = default);
}

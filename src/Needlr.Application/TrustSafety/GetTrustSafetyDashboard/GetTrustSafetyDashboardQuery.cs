using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.TrustSafety.GetTrustSafetyDashboard;

/// <summary>
/// Admin trust &amp; safety dashboard. Returns artists flagged by recent feedback patterns
/// or safety-keyword matches in free-text feedback (FEATURE_SPECS.md § Trust &amp; Safety).
/// </summary>
public sealed record GetTrustSafetyDashboardQuery : IQuery<TrustSafetyDashboardDto>;

public sealed record TrustSafetyDashboardDto(
    IReadOnlyList<FlaggedArtistDto> LowFeedbackAverages,
    IReadOnlyList<FlaggedArtistDto> RepeatNotBookingAgain,
    IReadOnlyList<FlaggedFeedbackDto> SafetyKeywordMatches);

public sealed record FlaggedArtistDto(
    Guid ArtistId,
    string DisplayName,
    int FeedbackCount,
    double AverageRating);

public sealed record FlaggedFeedbackDto(
    Guid FeedbackId,
    Guid BookingId,
    Guid ArtistId,
    string ArtistDisplayName,
    DateTime SubmittedAt,
    string MatchedKeyword,
    string FreeText);

namespace Needlr.Contracts.TrustSafety;

// ---- Customer feedback ----

public sealed record SubmitBookingFeedbackRequest(
    int CommunicationRating,
    int CleanlinessRating,
    int RespectedDesignBriefRating,
    bool WouldBookAgain,
    string? FreeText);

// ---- Admin moderation ----

public sealed record SuspendUserRequest(string Reason);
public sealed record WarnUserRequest(string Reason);

// ---- Admin dashboard ----

public sealed record FlaggedArtistResponse(
    Guid ArtistId,
    string DisplayName,
    int FeedbackCount,
    double AverageRating);

public sealed record FlaggedFeedbackResponse(
    Guid FeedbackId,
    Guid BookingId,
    Guid ArtistId,
    string ArtistDisplayName,
    DateTime SubmittedAt,
    string MatchedKeyword,
    string FreeText);

public sealed record TrustSafetyDashboardResponse(
    IReadOnlyList<FlaggedArtistResponse> LowFeedbackAverages,
    IReadOnlyList<FlaggedArtistResponse> RepeatNotBookingAgain,
    IReadOnlyList<FlaggedFeedbackResponse> SafetyKeywordMatches);

// ---- Behavioral signals on artist detail (already present in Artist contracts; mirror DTO here) ----

public sealed record BehavioralSignalsResponse(
    double? ResponseMedianHours,
    double? CompletionRatePercent,
    double? HealedPhotoRatePercent,
    double? RepeatClientRatePercent);

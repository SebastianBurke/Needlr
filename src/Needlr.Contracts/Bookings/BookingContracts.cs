namespace Needlr.Contracts.Bookings;

// ---- Requests ----

public sealed record RequestBookingRequest(
    Guid ArtistId,
    string BookingType,        // wire-format string of BookingType
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    string Description,
    string BodyPlacement,      // wire-format string of BodyPlacement
    int? ApproximateSizeCm = null,
    decimal? EstimatedTotalCad = null);

public sealed record AcceptBookingRequest(DateTime ConfirmedSessionDateUtc);

public sealed record DeclineBookingRequest(string Reason, string? Note);

public sealed record RespondWithMoreInfoRequest(
    string Description,
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    string BodyPlacement,
    int? ApproximateSizeCm = null,
    decimal? EstimatedTotalCad = null);

// ---- Responses ----

public sealed record BookingDetailResponse(
    Guid Id,
    Guid CustomerId,
    Guid ArtistId,
    Guid StudioId,
    string BookingType,
    string Status,
    DateTime RequestedAt,
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    string Description,
    string BodyPlacement,
    int? ApproximateSizeCm,
    decimal? EstimatedTotalCad,
    decimal DepositAmountCad,
    DateTime? AcceptedAt,
    DateTime? ConfirmedSessionDate,
    DateTime? CompletedAt,
    DateTime? DepositCapturedAt,
    string CancellationPolicySnapshot,
    string? DeclineReason,
    string? DeclineNote);

public sealed record BookingSummaryResponse(
    Guid Id,
    Guid CustomerId,
    Guid ArtistId,
    string BookingType,
    string Status,
    DateTime RequestedAt,
    DateOnly RequestedDate,
    DateTime? ConfirmedSessionDate);

public sealed record BookingPageResponse(
    IReadOnlyList<BookingSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNext,
    bool HasPrevious);

public sealed record CancelBookingResponse(decimal RefundedAmountCad);

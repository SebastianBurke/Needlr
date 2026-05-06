namespace Needlr.Contracts.Availability;

// ---- Pattern ----

public sealed record AvailabilityPatternDayRequest(
    string DayOfWeek,           // "Monday" .. "Sunday" (DayOfWeek enum)
    string Status,              // AvailabilityStatus: Available | Limited | Closed
    decimal? MaxSessionHours,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil);

public sealed record SetAvailabilityPatternRequest(IReadOnlyList<AvailabilityPatternDayRequest> Days);

public sealed record AvailabilityPatternDayResponse(
    Guid Id,
    string DayOfWeek,
    string Status,
    decimal? MaxSessionHours,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil);

public sealed record AvailabilityPatternResponse(IReadOnlyList<AvailabilityPatternDayResponse> Days);

// ---- Overrides ----

public sealed record AddAvailabilityOverrideRequest(
    DateOnly Date,
    string Status,
    decimal? MaxSessionHours,
    string? Reason);

public sealed record AvailabilityOverrideResponse(
    Guid Id,
    DateOnly Date,
    string Status,
    decimal? MaxSessionHours,
    string? Reason);

public sealed record AvailabilityOverridesResponse(IReadOnlyList<AvailabilityOverrideResponse> Items);

// ---- Booking windows ----

public sealed record CreateBookingWindowRequest(
    DateTime WindowOpensAt,
    DateTime WindowClosesAt,
    DateOnly TargetRangeStart,
    DateOnly TargetRangeEnd);

public sealed record BookingWindowResponse(
    Guid Id,
    DateTime WindowOpensAt,
    DateTime WindowClosesAt,
    DateOnly TargetRangeStart,
    DateOnly TargetRangeEnd);

public sealed record BookingWindowsResponse(IReadOnlyList<BookingWindowResponse> Items);

// ---- Lead times ----

public sealed record LeadTimeRequestItem(string BookingType, int MinimumDays);

public sealed record SetLeadTimesRequest(IReadOnlyList<LeadTimeRequestItem> LeadTimes);

public sealed record LeadTimeResponseItem(string BookingType, int MinimumDays);

public sealed record LeadTimesResponse(IReadOnlyList<LeadTimeResponseItem> Items);

// ---- Projection ----

public sealed record ProjectionDayResponse(DateOnly Date, bool IsBookable, decimal RemainingSessionHours);
public sealed record ProjectionResponse(IReadOnlyList<ProjectionDayResponse> Days);

// ---- iCal token ----

/// <summary>Returned by the rotate endpoint: the new token + the absolute feed URL clients can subscribe to.</summary>
public sealed record IcalFeedResponse(string Token, string FeedUrl);

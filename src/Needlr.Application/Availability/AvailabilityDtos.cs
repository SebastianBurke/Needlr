using Needlr.Domain.Enums;

namespace Needlr.Application.Availability;

/// <summary>
/// One day of an artist's recurring weekly availability. <c>EffectiveFrom</c> defaults to
/// today and <c>EffectiveUntil</c> to null (indefinite) when the caller doesn't supply them
/// — the typical case when an artist is just setting their normal weekly schedule.
/// </summary>
public sealed record AvailabilityPatternDayInput(
    DayOfWeek DayOfWeek,
    AvailabilityStatus Status,
    decimal? MaxSessionHours,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveUntil);

public sealed record AvailabilityPatternDayDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    AvailabilityStatus Status,
    decimal? MaxSessionHours,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil);

public sealed record AvailabilityOverrideDto(
    Guid Id,
    DateOnly Date,
    AvailabilityStatus Status,
    decimal? MaxSessionHours,
    string? Reason);

public sealed record BookingWindowDto(
    Guid Id,
    DateTime WindowOpensAt,
    DateTime WindowClosesAt,
    DateOnly TargetRangeStart,
    DateOnly TargetRangeEnd);

public sealed record LeadTimeDto(BookingType BookingType, int MinimumDays);

public sealed record ProjectionDayDto(
    DateOnly Date,
    bool IsBookable,
    decimal RemainingSessionHours);

using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings;

/// <summary>
/// Default deposit amount in CAD when neither the artist nor the request specifies one
/// (FEATURE_SPECS.md § Customer-initiated request flow). v1 ships a single platform-wide
/// default; per-artist overrides come in Phase 11 with Stripe Connect onboarding.
/// </summary>
public static class BookingDefaults
{
    public const decimal DefaultDepositCad = 100m;
}

public sealed record BookingDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerDisplayName,
    Guid ArtistId,
    string ArtistDisplayName,
    Guid StudioId,
    BookingType BookingType,
    BookingStatus Status,
    DateTime RequestedAt,
    DateOnly RequestedDate,
    decimal EstimatedDurationHours,
    string Description,
    BodyPlacement BodyPlacement,
    int? ApproximateSizeCm,
    decimal? EstimatedTotalCad,
    decimal DepositAmountCad,
    DateTime? AcceptedAt,
    DateTime? ConfirmedSessionDate,
    DateTime? CompletedAt,
    DateTime? DepositCapturedAt,
    CancellationPolicy CancellationPolicySnapshot,
    DeclineReason? DeclineReason,
    string? DeclineNote);

public sealed record BookingSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerDisplayName,
    Guid ArtistId,
    string ArtistDisplayName,
    BookingType BookingType,
    BookingStatus Status,
    DateTime RequestedAt,
    DateOnly RequestedDate,
    DateTime? ConfirmedSessionDate,
    int UnreadMessageCount);

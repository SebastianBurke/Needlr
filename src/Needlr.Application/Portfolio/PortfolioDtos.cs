using Needlr.Domain.Enums;

namespace Needlr.Application.Portfolio;

public sealed record PortfolioPieceSummaryDto(
    Guid Id,
    Guid ArtistId,
    string? Title,
    BodyPlacement BodyPlacement,
    int YearCompleted,
    ProgressionStatus ProgressionStatus,
    DateTime CreatedAt,
    string? FreshPhotoUrl,
    string? HealedPhotoUrl);

public sealed record PortfolioPieceDto(
    Guid Id,
    Guid ArtistId,
    string? Title,
    string? Description,
    BodyPlacement BodyPlacement,
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    ProgressionStatus ProgressionStatus,
    Guid? LinkedBookingId,
    DateTime CreatedAt,
    IReadOnlyList<TattooStyleDto> Styles,
    IReadOnlyList<string> FreeformTags,
    IReadOnlyList<SessionPhotoDto> Photos);

public sealed record TattooStyleDto(Guid Id, string Name, string Slug, bool IsCanonical);

public sealed record SessionPhotoDto(
    Guid Id,
    int Order,
    PhotoType PhotoType,
    string? ImageUrl,
    Guid UploadedByUserId,
    UploadedByRole UploadedByRole,
    DateTime UploadedAt,
    DateTime? LinkedSessionDate,
    bool IsHidden,
    string? HiddenReason);

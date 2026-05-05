namespace Needlr.Contracts.Portfolio;

// ---- Form payloads (file is sent as multipart "file") ----

public sealed record CreatePortfolioPieceRequest(
    string? Title,
    string? Description,
    string BodyPlacement,
    string StyleIds,            // comma-separated Guids; multipart string parts don't bind to arrays cleanly
    string? FreeformTags,       // comma-separated lowercase tags
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    string ProgressionStatus,
    Guid? LinkedBookingId);

public sealed record AddSessionPhotoFormRequest(
    string PhotoType,           // "Fresh" or "Healed"
    int Order,
    DateTime? LinkedSessionDate);

public sealed record UpdatePortfolioPieceRequest(
    string? Title,
    string? Description,
    string BodyPlacement,
    IReadOnlyList<Guid> StyleIds,
    IReadOnlyList<string> FreeformTags,
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    string ProgressionStatus);

public sealed record HideSessionPhotoRequest(string Reason);

// ---- Responses ----

public sealed record PortfolioPieceSummaryResponse(
    Guid Id,
    Guid ArtistId,
    string? Title,
    string BodyPlacement,
    int YearCompleted,
    string ProgressionStatus,
    DateTime CreatedAt,
    string? FreshPhotoUrl,
    string? HealedPhotoUrl);

public sealed record PortfolioPieceResponse(
    Guid Id,
    Guid ArtistId,
    string? Title,
    string? Description,
    string BodyPlacement,
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    string ProgressionStatus,
    Guid? LinkedBookingId,
    DateTime CreatedAt,
    IReadOnlyList<TattooStyleResponse> Styles,
    IReadOnlyList<string> FreeformTags,
    IReadOnlyList<SessionPhotoResponse> Photos);

public sealed record TattooStyleResponse(Guid Id, string Name, string Slug, bool IsCanonical);

public sealed record SessionPhotoResponse(
    Guid Id,
    int Order,
    string PhotoType,
    string? ImageUrl,
    Guid UploadedByUserId,
    string UploadedByRole,
    DateTime UploadedAt,
    DateTime? LinkedSessionDate,
    bool IsHidden,
    string? HiddenReason);

public sealed record PagedPortfolioResponse(
    IReadOnlyList<PortfolioPieceSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

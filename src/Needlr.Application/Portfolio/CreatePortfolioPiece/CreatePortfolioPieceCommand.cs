using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Portfolio.CreatePortfolioPiece;

/// <summary>
/// Artist creates a portfolio piece + initial Fresh photo. Multi-session pieces are still
/// created with a single initial photo here; subsequent session photos are added via
/// <see cref="AddSessionPhoto.AddSessionPhotoCommand"/>.
/// </summary>
public sealed record CreatePortfolioPieceCommand(
    string? Title,
    string? Description,
    BodyPlacement BodyPlacement,
    IReadOnlyList<Guid> StyleIds,
    IReadOnlyList<string> FreeformTags,
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    ProgressionStatus ProgressionStatus,
    Guid? LinkedBookingId,
    Stream FileContent,
    string ContentType,
    string OriginalFilename) : ICommand<Guid>;

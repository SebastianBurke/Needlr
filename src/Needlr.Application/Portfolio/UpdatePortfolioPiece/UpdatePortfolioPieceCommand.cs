using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Portfolio.UpdatePortfolioPiece;

public sealed record UpdatePortfolioPieceCommand(
    Guid PortfolioPieceId,
    string? Title,
    string? Description,
    BodyPlacement BodyPlacement,
    IReadOnlyList<Guid> StyleIds,
    IReadOnlyList<string> FreeformTags,
    int? ApproximateSizeCm,
    decimal? EstimatedSessionLengthHours,
    int YearCompleted,
    ProgressionStatus ProgressionStatus) : ICommand;

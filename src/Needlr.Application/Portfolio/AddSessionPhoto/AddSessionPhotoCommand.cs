using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Portfolio.AddSessionPhoto;

public sealed record AddSessionPhotoCommand(
    Guid PortfolioPieceId,
    PhotoType PhotoType,
    int Order,
    DateTime? LinkedSessionDate,
    Stream FileContent,
    string ContentType,
    string OriginalFilename) : ICommand<Guid>;

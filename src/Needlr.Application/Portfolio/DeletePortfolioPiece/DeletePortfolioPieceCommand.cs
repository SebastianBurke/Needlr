using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.DeletePortfolioPiece;

public sealed record DeletePortfolioPieceCommand(Guid PortfolioPieceId) : ICommand;

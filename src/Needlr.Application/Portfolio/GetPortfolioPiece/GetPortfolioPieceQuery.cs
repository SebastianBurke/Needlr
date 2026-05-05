using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.GetPortfolioPiece;

public sealed record GetPortfolioPieceQuery(Guid PortfolioPieceId) : IQuery<PortfolioPieceDto>;

using FluentValidation;

namespace Needlr.Application.Portfolio.DeletePortfolioPiece;

public sealed class DeletePortfolioPieceCommandValidator : AbstractValidator<DeletePortfolioPieceCommand>
{
    public DeletePortfolioPieceCommandValidator()
    {
        RuleFor(x => x.PortfolioPieceId).NotEmpty();
    }
}

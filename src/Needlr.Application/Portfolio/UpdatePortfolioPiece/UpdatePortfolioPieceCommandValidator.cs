using FluentValidation;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.UpdatePortfolioPiece;

public sealed class UpdatePortfolioPieceCommandValidator : AbstractValidator<UpdatePortfolioPieceCommand>
{
    public UpdatePortfolioPieceCommandValidator()
    {
        RuleFor(x => x.PortfolioPieceId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(PortfolioPiece.TitleMaxLength).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(PortfolioPiece.DescriptionMaxLength).When(x => x.Description is not null);
        RuleFor(x => x.StyleIds).NotEmpty();
        RuleFor(x => x.FreeformTags)
            .Must(tags => tags.Count <= PortfolioPiece.MaxFreeformTags)
            .WithMessage($"At most {PortfolioPiece.MaxFreeformTags} freeform tags are allowed.");
        RuleFor(x => x.YearCompleted)
            .InclusiveBetween(PortfolioPiece.MinYearCompleted, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.ApproximateSizeCm).GreaterThan(0).When(x => x.ApproximateSizeCm.HasValue);
        RuleFor(x => x.EstimatedSessionLengthHours).GreaterThan(0).When(x => x.EstimatedSessionLengthHours.HasValue);
    }
}

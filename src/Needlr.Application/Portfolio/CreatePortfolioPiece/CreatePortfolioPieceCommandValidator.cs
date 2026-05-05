using FluentValidation;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.CreatePortfolioPiece;

public sealed class CreatePortfolioPieceCommandValidator : AbstractValidator<CreatePortfolioPieceCommand>
{
    public CreatePortfolioPieceCommandValidator()
    {
        RuleFor(x => x.Title).MaximumLength(PortfolioPiece.TitleMaxLength).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(PortfolioPiece.DescriptionMaxLength).When(x => x.Description is not null);

        RuleFor(x => x.StyleIds)
            .NotEmpty()
            .WithMessage("At least one style is required.");

        RuleFor(x => x.FreeformTags)
            .Must(tags => tags.Count <= PortfolioPiece.MaxFreeformTags)
            .WithMessage($"At most {PortfolioPiece.MaxFreeformTags} freeform tags are allowed.");

        RuleForEach(x => x.FreeformTags)
            .NotEmpty()
            .MaximumLength(PortfolioPiece.FreeformTagMaxLength);

        RuleFor(x => x.ApproximateSizeCm).GreaterThan(0).When(x => x.ApproximateSizeCm.HasValue);
        RuleFor(x => x.EstimatedSessionLengthHours).GreaterThan(0).When(x => x.EstimatedSessionLengthHours.HasValue);

        RuleFor(x => x.YearCompleted)
            .InclusiveBetween(PortfolioPiece.MinYearCompleted, DateTime.UtcNow.Year + 1);

        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.OriginalFilename).NotEmpty();
        RuleFor(x => x.FileContent).NotNull();
    }
}

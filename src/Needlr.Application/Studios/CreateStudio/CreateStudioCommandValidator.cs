using FluentValidation;
using Needlr.Domain.Studios;

namespace Needlr.Application.Studios.CreateStudio;

public sealed class CreateStudioCommandValidator : AbstractValidator<CreateStudioCommand>
{
    public CreateStudioCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Studio.NameMaxLength);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(Studio.AddressMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Studio.DescriptionMaxLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.Location)
            .NotNull()
            .Must(p => p.IsValid)
            .WithMessage("Location latitude/longitude is out of valid range.");
    }
}

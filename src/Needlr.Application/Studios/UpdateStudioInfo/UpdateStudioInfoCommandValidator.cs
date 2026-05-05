using FluentValidation;
using Needlr.Domain.Studios;

namespace Needlr.Application.Studios.UpdateStudioInfo;

public sealed class UpdateStudioInfoCommandValidator : AbstractValidator<UpdateStudioInfoCommand>
{
    public UpdateStudioInfoCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Studio.NameMaxLength);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(Studio.AddressMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(Studio.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}

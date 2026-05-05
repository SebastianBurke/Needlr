using FluentValidation;

namespace Needlr.Application.Affiliations.RemoveAffiliation;

public sealed class RemoveAffiliationCommandValidator : AbstractValidator<RemoveAffiliationCommand>
{
    public RemoveAffiliationCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
    }
}

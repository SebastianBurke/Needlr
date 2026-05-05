using FluentValidation;

namespace Needlr.Application.Affiliations.SetPrimaryAffiliation;

public sealed class SetPrimaryAffiliationCommandValidator : AbstractValidator<SetPrimaryAffiliationCommand>
{
    public SetPrimaryAffiliationCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
    }
}

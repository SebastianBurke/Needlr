using FluentValidation;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.ChangeAffiliationRole;

public sealed class ChangeAffiliationRoleCommandValidator : AbstractValidator<ChangeAffiliationRoleCommand>
{
    public ChangeAffiliationRoleCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
        RuleFor(x => x.NewRole)
            .Must(r => r is AffiliationRole.Admin or AffiliationRole.Member)
            .WithMessage("Role can only be changed to Admin or Member. Founder must be ceded explicitly.");
    }
}

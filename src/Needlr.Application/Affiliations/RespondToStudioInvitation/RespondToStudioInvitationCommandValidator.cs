using FluentValidation;

namespace Needlr.Application.Affiliations.RespondToStudioInvitation;

public sealed class RespondToStudioInvitationCommandValidator : AbstractValidator<RespondToStudioInvitationCommand>
{
    public RespondToStudioInvitationCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
    }
}

using FluentValidation;

namespace Needlr.Application.Affiliations.RespondToJoinRequest;

public sealed class RespondToJoinRequestCommandValidator : AbstractValidator<RespondToJoinRequestCommand>
{
    public RespondToJoinRequestCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
    }
}

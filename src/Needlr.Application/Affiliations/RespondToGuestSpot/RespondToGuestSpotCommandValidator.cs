using FluentValidation;

namespace Needlr.Application.Affiliations.RespondToGuestSpot;

public sealed class RespondToGuestSpotCommandValidator : AbstractValidator<RespondToGuestSpotCommand>
{
    public RespondToGuestSpotCommandValidator()
    {
        RuleFor(x => x.AffiliationId).NotEmpty();
    }
}

using FluentValidation;

namespace Needlr.Application.Affiliations.RequestGuestSpot;

public sealed class RequestGuestSpotCommandValidator : AbstractValidator<RequestGuestSpotCommand>
{
    public RequestGuestSpotCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }
}

using FluentValidation;
using Needlr.Domain.Portfolio;

namespace Needlr.Application.Portfolio.HideSessionPhoto;

public sealed class HideSessionPhotoCommandValidator : AbstractValidator<HideSessionPhotoCommand>
{
    public HideSessionPhotoCommandValidator()
    {
        RuleFor(x => x.PhotoId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(SessionPhoto.HiddenReasonMaxLength)
            .WithMessage("A specific reason is required (≥ 10 chars). Hiding for non-content-policy reasons is not allowed.");
    }
}

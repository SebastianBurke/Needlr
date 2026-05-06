using FluentValidation;
using Needlr.Domain.Identity;

namespace Needlr.Application.Customers.UpdateMyProfile;

public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(CustomerProfile.DisplayNameMaxLength)
            .WithMessage($"Display name must be {CustomerProfile.DisplayNameMaxLength} characters or fewer.");
    }
}

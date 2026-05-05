using FluentValidation;
using Needlr.Domain.Identity;

namespace Needlr.Application.Auth.RegisterCustomer;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(CustomerProfile.DisplayNameMaxLength);
    }
}

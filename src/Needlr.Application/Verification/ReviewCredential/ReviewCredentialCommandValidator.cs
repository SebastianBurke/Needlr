using FluentValidation;

namespace Needlr.Application.Verification.ReviewCredential;

public sealed class ReviewCredentialCommandValidator : AbstractValidator<ReviewCredentialCommand>
{
    public ReviewCredentialCommandValidator()
    {
        RuleFor(x => x.CredentialId).NotEmpty();
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .MaximumLength(1000)
            .When(x => !x.Approve)
            .WithMessage("RejectionReason is required when rejecting a credential.");
    }
}

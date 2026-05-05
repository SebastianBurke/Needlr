using FluentValidation;

namespace Needlr.Application.Verification.UploadStudioCredential;

public sealed class UploadStudioCredentialCommandValidator : AbstractValidator<UploadStudioCredentialCommand>
{
    public UploadStudioCredentialCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.JurisdictionId).NotEmpty();
        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.IssuedDate)
            .WithMessage("Expiry date must be after the issued date.");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OriginalFilename).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FileContent).NotNull();
    }
}

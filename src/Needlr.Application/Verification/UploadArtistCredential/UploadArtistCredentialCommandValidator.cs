using FluentValidation;

namespace Needlr.Application.Verification.UploadArtistCredential;

public sealed class UploadArtistCredentialCommandValidator : AbstractValidator<UploadArtistCredentialCommand>
{
    public UploadArtistCredentialCommandValidator()
    {
        RuleFor(x => x.JurisdictionId).NotEmpty();
        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.IssuedDate)
            .WithMessage("Expiry date must be after the issued date.");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OriginalFilename).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FileContent).NotNull();
    }
}

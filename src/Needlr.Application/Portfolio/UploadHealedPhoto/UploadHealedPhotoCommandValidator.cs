using FluentValidation;

namespace Needlr.Application.Portfolio.UploadHealedPhoto;

public sealed class UploadHealedPhotoCommandValidator : AbstractValidator<UploadHealedPhotoCommand>
{
    public UploadHealedPhotoCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.OriginalFilename).NotEmpty();
        RuleFor(x => x.FileContent).NotNull();
    }
}

using FluentValidation;

namespace Needlr.Application.Portfolio.AddSessionPhoto;

public sealed class AddSessionPhotoCommandValidator : AbstractValidator<AddSessionPhotoCommand>
{
    public AddSessionPhotoCommandValidator()
    {
        RuleFor(x => x.PortfolioPieceId).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.OriginalFilename).NotEmpty();
        RuleFor(x => x.FileContent).NotNull();
    }
}

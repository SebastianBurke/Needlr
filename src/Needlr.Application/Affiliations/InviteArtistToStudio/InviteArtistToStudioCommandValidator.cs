using FluentValidation;

namespace Needlr.Application.Affiliations.InviteArtistToStudio;

public sealed class InviteArtistToStudioCommandValidator : AbstractValidator<InviteArtistToStudioCommand>
{
    public InviteArtistToStudioCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.ArtistId).NotEmpty();
    }
}

using FluentValidation;
using Needlr.Domain.Identity;

namespace Needlr.Application.Artists.UpdateArtistProfile;

public sealed class UpdateArtistProfileValidator : AbstractValidator<UpdateArtistProfileCommand>
{
    public UpdateArtistProfileValidator()
    {
        RuleFor(x => x.Bio)
            .NotNull()
            .MaximumLength(Artist.BioMaxLength)
            .WithMessage($"Bio must be {Artist.BioMaxLength} characters or fewer.");
        RuleFor(x => x.YearsExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be 0 or more.")
            .LessThanOrEqualTo(80).WithMessage("Years of experience must be 80 or fewer.");
        RuleFor(x => x.HourlyRateCad)
            .GreaterThanOrEqualTo(0).When(x => x.HourlyRateCad.HasValue)
            .WithMessage("Hourly rate must be 0 or more.");
        RuleFor(x => x.ShopMinimumCad)
            .GreaterThanOrEqualTo(0).When(x => x.ShopMinimumCad.HasValue)
            .WithMessage("Shop minimum must be 0 or more.");
        RuleFor(x => x.CancellationPolicy)
            .IsInEnum().WithMessage("Invalid cancellation policy.");
    }
}

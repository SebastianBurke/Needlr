using FluentValidation;
using Needlr.Domain.Availability;

namespace Needlr.Application.Availability.AddAvailabilityOverride;

public sealed class AddAvailabilityOverrideCommandValidator : AbstractValidator<AddAvailabilityOverrideCommand>
{
    public AddAvailabilityOverrideCommandValidator()
    {
        RuleFor(x => x.MaxSessionHours)
            .InclusiveBetween(0.01m, AvailabilityOverride.MaxSessionHoursMax)
            .When(x => x.MaxSessionHours.HasValue);

        RuleFor(x => x.Reason)
            .MaximumLength(AvailabilityOverride.ReasonMaxLength)
            .When(x => x.Reason is not null);
    }
}

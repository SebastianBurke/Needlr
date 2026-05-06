using FluentValidation;
using Needlr.Domain.Availability;

namespace Needlr.Application.Availability.SetAvailabilityPattern;

public sealed class SetAvailabilityPatternCommandValidator : AbstractValidator<SetAvailabilityPatternCommand>
{
    public SetAvailabilityPatternCommandValidator()
    {
        RuleFor(x => x.Days).NotNull();

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.MaxSessionHours)
                .InclusiveBetween(0.01m, AvailabilityPattern.MaxSessionHoursMax)
                .When(d => d.MaxSessionHours.HasValue);

            day.RuleFor(d => d.EffectiveUntil)
                .GreaterThanOrEqualTo(d => d.EffectiveFrom)
                .When(d => d.EffectiveFrom.HasValue && d.EffectiveUntil.HasValue);
        });

        // No two rows for the same DayOfWeek with overlapping effective windows in the same
        // request — keeps the projector deterministic without intra-row tie-break logic.
        RuleFor(x => x.Days)
            .Must(days => days
                .GroupBy(d => d.DayOfWeek)
                .All(g => g.Count() == 1))
            .WithMessage("Cannot supply more than one pattern row per day-of-week in a single request.");
    }
}

using FluentValidation;
using Needlr.Domain.Identity;

namespace Needlr.Application.Availability.SetLeadTimes;

public sealed class SetLeadTimesCommandValidator : AbstractValidator<SetLeadTimesCommand>
{
    public SetLeadTimesCommandValidator()
    {
        RuleFor(x => x.LeadTimes).NotNull();

        RuleForEach(x => x.LeadTimes).ChildRules(item =>
        {
            item.RuleFor(lt => lt.MinimumDays)
                .InclusiveBetween(0, ArtistLeadTime.MaxMinimumDays);
        });

        RuleFor(x => x.LeadTimes)
            .Must(items => items.GroupBy(lt => lt.BookingType).All(g => g.Count() == 1))
            .WithMessage("Cannot supply more than one lead time per BookingType.");
    }
}

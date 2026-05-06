using FluentValidation;
using Needlr.Domain.Bookings;

namespace Needlr.Application.Bookings.RespondWithMoreInfo;

public sealed class RespondWithMoreInfoCommandValidator : AbstractValidator<RespondWithMoreInfoCommand>
{
    public RespondWithMoreInfoCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(Booking.DescriptionMaxLength);
        RuleFor(x => x.EstimatedDurationHours).InclusiveBetween(0.25m, Booking.MaxEstimatedDurationHours);
        RuleFor(x => x.ApproximateSizeCm).GreaterThanOrEqualTo(0).When(x => x.ApproximateSizeCm.HasValue);
        RuleFor(x => x.EstimatedTotalCad).GreaterThanOrEqualTo(0).When(x => x.EstimatedTotalCad.HasValue);
    }
}

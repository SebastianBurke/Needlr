using FluentValidation;
using Needlr.Domain.Bookings;

namespace Needlr.Application.TrustSafety.SubmitBookingFeedback;

public sealed class SubmitBookingFeedbackCommandValidator : AbstractValidator<SubmitBookingFeedbackCommand>
{
    public SubmitBookingFeedbackCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.CommunicationRating).InclusiveBetween(BookingFeedback.MinRating, BookingFeedback.MaxRating);
        RuleFor(x => x.CleanlinessRating).InclusiveBetween(BookingFeedback.MinRating, BookingFeedback.MaxRating);
        RuleFor(x => x.RespectedDesignBriefRating).InclusiveBetween(BookingFeedback.MinRating, BookingFeedback.MaxRating);
        RuleFor(x => x.FreeText).MaximumLength(BookingFeedback.FreeTextMaxLength)
            .When(x => x.FreeText is not null);
    }
}

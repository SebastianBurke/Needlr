using FluentValidation;
using Needlr.Domain.Bookings;

namespace Needlr.Application.Bookings.DeclineBooking;

public sealed class DeclineBookingCommandValidator : AbstractValidator<DeclineBookingCommand>
{
    public DeclineBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Note)
            .MaximumLength(Booking.DeclineNoteMaxLength)
            .When(x => x.Note is not null);
    }
}

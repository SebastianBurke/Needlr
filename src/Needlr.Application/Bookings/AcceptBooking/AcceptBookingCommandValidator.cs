using FluentValidation;

namespace Needlr.Application.Bookings.AcceptBooking;

public sealed class AcceptBookingCommandValidator : AbstractValidator<AcceptBookingCommand>
{
    public AcceptBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.ConfirmedSessionDateUtc)
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("ConfirmedSessionDateUtc must be UTC.");
    }
}

using FluentValidation;

namespace Needlr.Application.Availability.CreateBookingWindow;

public sealed class CreateBookingWindowCommandValidator : AbstractValidator<CreateBookingWindowCommand>
{
    public CreateBookingWindowCommandValidator()
    {
        RuleFor(x => x.WindowOpensAt)
            .Must(t => t.Kind == DateTimeKind.Utc)
            .WithMessage("WindowOpensAt must be UTC.");
        RuleFor(x => x.WindowClosesAt)
            .Must(t => t.Kind == DateTimeKind.Utc)
            .WithMessage("WindowClosesAt must be UTC.");
        RuleFor(x => x.WindowClosesAt)
            .GreaterThan(x => x.WindowOpensAt);
        RuleFor(x => x.TargetRangeEnd)
            .GreaterThanOrEqualTo(x => x.TargetRangeStart);
    }
}

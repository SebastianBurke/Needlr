using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.CreateBookingWindow;

/// <summary>
/// Opens a booking window for the calling artist. Requests are accepted only during
/// <c>[WindowOpensAt, WindowClosesAt]</c> for sessions in <c>[TargetRangeStart, TargetRangeEnd]</c>.
/// Multiple windows can coexist; their target ranges may overlap (the projector treats them as union).
/// </summary>
public sealed record CreateBookingWindowCommand(
    DateTime WindowOpensAt,
    DateTime WindowClosesAt,
    DateOnly TargetRangeStart,
    DateOnly TargetRangeEnd) : ICommand<Guid>;

using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.SetAvailabilityPattern;

/// <summary>
/// Replaces the calling artist's recurring weekly availability pattern wholesale. Empty list
/// means the artist has no recurring availability (every day defaults to Closed). The
/// projector is rebuilt for the rolling 90-day window after the swap.
/// </summary>
public sealed record SetAvailabilityPatternCommand(
    IReadOnlyList<AvailabilityPatternDayInput> Days) : ICommand;

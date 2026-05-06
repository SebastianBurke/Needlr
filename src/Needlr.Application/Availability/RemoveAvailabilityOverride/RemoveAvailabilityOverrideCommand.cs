using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.RemoveAvailabilityOverride;

public sealed record RemoveAvailabilityOverrideCommand(DateOnly Date) : ICommand;

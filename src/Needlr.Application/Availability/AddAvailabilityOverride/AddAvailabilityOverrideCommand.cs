using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Availability.AddAvailabilityOverride;

/// <summary>
/// Adds a one-off availability exception for the calling artist on the given date. If a row
/// already exists for that date, it is replaced. Recomputes the projection rolling window.
/// </summary>
public sealed record AddAvailabilityOverrideCommand(
    DateOnly Date,
    AvailabilityStatus Status,
    decimal? MaxSessionHours,
    string? Reason) : ICommand<Guid>;

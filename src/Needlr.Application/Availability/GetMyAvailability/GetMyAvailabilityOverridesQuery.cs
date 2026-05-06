using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.GetMyAvailability;

/// <summary>
/// Lists overrides for the calling artist. Optional date filter <c>[from, to]</c> matches
/// only rows whose <c>Date</c> falls in the inclusive range — defaults to all rows.
/// </summary>
public sealed record GetMyAvailabilityOverridesQuery(DateOnly? From = null, DateOnly? To = null)
    : IQuery<IReadOnlyList<AvailabilityOverrideDto>>;

using Needlr.Application.Messaging;

namespace Needlr.Application.Studios.SetStudioWalkIns;

/// <summary>
/// Toggles a studio's <c>AcceptsWalkIns</c> flag. Single-purpose endpoint mirroring the
/// per-artist accepting-bookings toggle, so the FE doesn't have to round-trip the rest of
/// <c>UpdateStudioInfo</c>'s required fields just to flip a boolean.
/// </summary>
public sealed record SetStudioWalkInsCommand(Guid StudioId, bool AcceptsWalkIns) : ICommand;

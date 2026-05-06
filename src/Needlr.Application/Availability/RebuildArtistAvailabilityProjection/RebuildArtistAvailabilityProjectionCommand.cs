using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.RebuildArtistAvailabilityProjection;

/// <summary>
/// Recomputes the rolling 90-day projection for one artist on demand. Useful after the
/// artist edits availability, after a booking flip in Phase 10, or as an admin tool.
/// </summary>
public sealed record RebuildArtistAvailabilityProjectionCommand(Guid ArtistId) : ICommand;

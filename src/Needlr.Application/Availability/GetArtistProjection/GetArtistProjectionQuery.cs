using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.GetArtistProjection;

/// <summary>
/// Returns precomputed availability for an artist over [from, to]. Public read so the
/// booking-request UI can show a per-day capacity hint before the customer submits.
/// </summary>
public sealed record GetArtistProjectionQuery(
    Guid ArtistId,
    DateOnly From,
    DateOnly To) : IQuery<IReadOnlyList<ProjectionDayDto>>;

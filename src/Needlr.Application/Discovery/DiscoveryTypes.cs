using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Pagination;
using Needlr.Domain.Enums;

namespace Needlr.Application.Discovery;

/// <summary>
/// Search criteria for the discovery map / list. Per FEATURE_SPECS.md § Discovery, studios
/// are the primary result entity; the per-artist filters (style, accepting bookings) collapse
/// to "studio has at least one Active artist matching". Walk-ins is a venue-level flag on
/// the studio itself.
/// </summary>
public sealed record DiscoverySearchCriteria(
    BoundingBox Bounds,
    GeoPoint Center,
    IReadOnlyList<Guid>? StyleIds,
    bool VerifiedOnly,
    DateOnly? AvailabilityFrom,
    DateOnly? AvailabilityTo,
    bool AcceptsWalkInsOnly,
    PageRequest Page);

/// <summary>One row in the discovery list/map response.</summary>
public sealed record DiscoveryStudioDto(
    Guid Id,
    string Name,
    string Address,
    StudioType StudioType,
    GeoPoint Location,
    /// <summary>Cartesian distance from <see cref="DiscoverySearchCriteria.Center"/> in WGS84
    /// degrees. Monotonic for sort, but NOT meters — use for ordering only. Phase 8 doesn't
    /// surface a meters-distance to the UI; once we move to <c>geography</c> typing we can add
    /// it.</summary>
    double DistanceFromCenter,
    bool IsVerified,
    bool HasSubmittedDocuments,
    int ActiveArtistCount);

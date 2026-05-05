using Needlr.Contracts.Studios;

namespace Needlr.Contracts.Discovery;

public sealed record DiscoveryStudioResponse(
    Guid Id,
    string Name,
    string Address,
    string StudioType,
    GeoPointDto Location,
    double DistanceFromCenter,
    bool IsVerified,
    bool HasSubmittedDocuments,
    int ActiveArtistCount);

public sealed record DiscoveryPageResponse(
    IReadOnlyList<DiscoveryStudioResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

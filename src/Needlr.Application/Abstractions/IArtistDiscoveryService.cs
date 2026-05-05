using Needlr.Application.Common.Pagination;
using Needlr.Application.Discovery;

namespace Needlr.Application.Abstractions;

/// <summary>
/// Spatial discovery query. Owned by Infrastructure because it depends on PostGIS /
/// NetTopologySuite spatial queries that can't be reasonably expressed without EF + Npgsql.
/// </summary>
public interface IArtistDiscoveryService
{
    Task<PagedResult<DiscoveryStudioDto>> SearchAsync(
        DiscoverySearchCriteria criteria, CancellationToken cancellationToken = default);
}

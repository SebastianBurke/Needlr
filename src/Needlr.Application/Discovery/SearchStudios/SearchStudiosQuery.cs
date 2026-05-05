using Needlr.Application.Common.Pagination;
using Needlr.Application.Messaging;

namespace Needlr.Application.Discovery.SearchStudios;

public sealed record SearchStudiosQuery(DiscoverySearchCriteria Criteria)
    : IQuery<PagedResult<DiscoveryStudioDto>>;

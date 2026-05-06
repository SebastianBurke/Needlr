using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.GetMyActiveThreads;

public sealed record GetMyActiveThreadsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ThreadDto>>;

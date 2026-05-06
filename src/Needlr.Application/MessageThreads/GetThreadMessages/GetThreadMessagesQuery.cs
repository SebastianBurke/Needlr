using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.GetThreadMessages;

public sealed record GetThreadMessagesQuery(
    Guid ThreadId,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedResult<MessageDto>>;

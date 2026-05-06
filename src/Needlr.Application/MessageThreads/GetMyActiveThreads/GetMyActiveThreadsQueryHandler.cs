using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;

namespace Needlr.Application.MessageThreads.GetMyActiveThreads;

internal sealed class GetMyActiveThreadsQueryHandler(
    ICurrentUser currentUser,
    IMessageThreadRepository threads)
    : IRequestHandler<GetMyActiveThreadsQuery, Result<PagedResult<ThreadDto>>>
{
    public async Task<Result<PagedResult<ThreadDto>>> Handle(
        GetMyActiveThreadsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<PagedResult<ThreadDto>>.Failure(Error.Unauthorized());

        var page = await threads.ListActiveForUserAsync(
            currentUser.UserId.Value,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);

        var dtos = page.Items
            .Select(t => new ThreadDto(t.Id, t.BookingId, t.OpenedAt, t.LockedAt, t.Status, null))
            .ToList();
        return Result<PagedResult<ThreadDto>>.Success(
            new PagedResult<ThreadDto>(dtos, page.Page, page.PageSize, page.TotalCount));
    }
}

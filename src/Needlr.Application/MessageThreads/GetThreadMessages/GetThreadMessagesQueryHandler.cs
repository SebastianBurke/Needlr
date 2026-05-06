using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.GetThreadMessages;

internal sealed class GetThreadMessagesQueryHandler(
    ICurrentUser currentUser,
    IMessageThreadRepository threads,
    IMessageRepository messages,
    IArtistRepository artists)
    : IRequestHandler<GetThreadMessagesQuery, Result<PagedResult<MessageDto>>>
{
    public async Task<Result<PagedResult<MessageDto>>> Handle(
        GetThreadMessagesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<PagedResult<MessageDto>>.Failure(Error.Unauthorized());

        var pair = await threads.GetWithBookingAsync(request.ThreadId, cancellationToken);
        if (pair is null)
            return Result<PagedResult<MessageDto>>.Failure(Error.NotFound("Thread"));

        var role = await ThreadParty.ResolveAsync(
            currentUser.UserId.Value, pair.Value.Booking, artists, cancellationToken);
        var isAdmin = currentUser.IsInRole(UserRole.Admin);
        if (role is null && !isAdmin)
            return Result<PagedResult<MessageDto>>.Failure(Error.Forbidden("Not a party to this thread."));

        var page = await messages.ListByThreadAsync(
            request.ThreadId, new PageRequest(request.Page, request.PageSize), cancellationToken);

        var dtos = page.Items.Select(m => new MessageDto(
            m.Id,
            m.ThreadId,
            m.SenderId,
            m.Body,
            m.SentAt,
            m.ReadAt,
            m.IsReportedFlag,
            m.Attachments.Select(a => new MessageAttachmentDto(
                a.Id, a.Url, a.OriginalFilename, a.MimeType, a.SizeBytes, a.UploadedAt)).ToList())).ToList();
        return Result<PagedResult<MessageDto>>.Success(
            new PagedResult<MessageDto>(dtos, page.Page, page.PageSize, page.TotalCount));
    }
}

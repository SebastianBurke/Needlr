using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Pagination;
using Needlr.Application.MessageThreads;
using Needlr.Application.MessageThreads.GetMyActiveThreads;
using Needlr.Application.MessageThreads.GetThreadMessages;
using Needlr.Application.MessageThreads.GetUnreadCount;
using Needlr.Application.MessageThreads.MarkMessageRead;
using Needlr.Application.MessageThreads.ReportMessage;
using Needlr.Application.MessageThreads.SendMessage;
using Needlr.Application.MessageThreads.UploadMessageAttachment;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Authorize]
public sealed class MessagesController(IMediator mediator) : ControllerBase
{
    private const long MaxAttachmentBytes = 10L * 1024 * 1024;

    private readonly IMediator _mediator = mediator;

    // ---- Threads ----

    [HttpGet("/api/threads/mine")]
    public async Task<IActionResult> ListMyThreads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetMyActiveThreadsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult(ToThreadPage);
    }

    [HttpGet("/api/threads/{threadId:guid}/messages")]
    public async Task<IActionResult> ListMessages(
        Guid threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetThreadMessagesQuery(threadId, page, pageSize), cancellationToken);
        return result.ToActionResult(ToMessagePage);
    }

    [HttpPost("/api/threads/{threadId:guid}/messages")]
    public async Task<IActionResult> Send(
        Guid threadId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendMessageCommand(threadId, request.Body), cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    // ---- Per-message actions ----

    [HttpPost("/api/messages/{messageId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkMessageReadCommand(messageId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("/api/messages/{messageId:guid}/report")]
    public async Task<IActionResult> Report(
        Guid messageId,
        [FromBody] ReportMessageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReportMessageCommand(
            messageId, ParseEnum<MessageReportReason>(request.Reason), request.Note);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpPost("/api/messages/{messageId:guid}/attachments")]
    [RequestSizeLimit(MaxAttachmentBytes)]
    public async Task<IActionResult> Attach(
        Guid messageId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadMessageAttachmentCommand(
            messageId, stream, file.ContentType, file.FileName, file.Length);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    // ---- Inbox-level reads ----

    [HttpGet("/api/messages/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUnreadMessageCountQuery(), cancellationToken);
        return result.ToActionResult(count => new UnreadCountResponse(count));
    }

    // ---- helpers ----

    private static MessagePageResponse ToMessagePage(PagedResult<MessageDto> p) => new(
        p.Items.Select(m => new MessageResponse(
            m.Id, m.ThreadId, m.SenderId, m.Body, m.SentAt, m.ReadAt, m.IsReportedFlag,
            m.Attachments.Select(a => new MessageAttachmentResponse(
                a.Id, a.Url, a.OriginalFilename, a.MimeType, a.SizeBytes, a.UploadedAt)).ToList())).ToList(),
        p.Page, p.PageSize, p.TotalCount, p.HasNext, p.HasPrevious);

    private static ThreadPageResponse ToThreadPage(PagedResult<ThreadDto> p) => new(
        p.Items.Select(t => new ThreadResponse(
            t.Id, t.BookingId, t.OpenedAt, t.LockedAt, t.Status.ToString())).ToList(),
        p.Page, p.PageSize, p.TotalCount, p.HasNext, p.HasPrevious);

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

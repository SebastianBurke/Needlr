using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Pagination;
using Needlr.Application.MessageThreads.HideMessage;
using Needlr.Application.MessageThreads.ResolveMessageReport;
using Needlr.Application.Verification;
using Needlr.Application.Verification.GetVerificationQueue;
using Needlr.Application.Verification.ReviewCredential;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Verification;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("verification-queue")]
    public async Task<IActionResult> GetVerificationQueue(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVerificationQueueQuery(), cancellationToken);
        return result.ToActionResult(items => items.Select(ToResponse).ToList());
    }

    [HttpPost("credentials/{kind}/{id:guid}/review")]
    public async Task<IActionResult> ReviewCredential(
        string kind,
        Guid id,
        [FromBody] ReviewCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var parsedKind = Enum.TryParse<CredentialKind>(kind, ignoreCase: true, out var k)
            ? k
            : throw new ArgumentException($"Invalid credential kind: '{kind}'. Expected 'Studio' or 'Artist'.");

        var result = await _mediator.Send(
            new ReviewCredentialCommand(parsedKind, id, request.Approve, request.RejectionReason),
            cancellationToken);
        return result.ToActionResult();
    }

    private static VerificationQueueItemResponse ToResponse(VerificationQueueItemDto dto) => new(
        dto.Id,
        dto.Kind.ToString(),
        dto.OwnerEntityId,
        dto.CredentialType,
        dto.DocumentUrl,
        dto.IssuedDate,
        dto.ExpiryDate);

    // ---- Messaging moderation (Phase 12) ----

    [HttpPost("messages/{messageId:guid}/hide")]
    public async Task<IActionResult> HideMessage(
        Guid messageId,
        [FromBody] HideMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new HideMessageCommand(messageId, request.Reason), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("message-reports/{reportId:guid}/resolve")]
    public async Task<IActionResult> ResolveMessageReport(
        Guid reportId,
        [FromBody] ResolveReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResolveMessageReportCommand(reportId,
                ParseEnum<MessageReportResolution>(request.Resolution)),
            cancellationToken);
        return result.ToActionResult();
    }

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

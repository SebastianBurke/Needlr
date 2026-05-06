using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Pagination;
using Needlr.Application.MessageThreads.HideMessage;
using Needlr.Application.MessageThreads.ResolveMessageReport;
using Needlr.Application.Moderation.SearchUsers;
using Needlr.Application.Verification;
using Needlr.Application.Verification.GetVerificationQueue;
using Needlr.Application.Verification.ReviewCredential;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.TrustSafety;
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

    // ---- Trust & Safety (Phase 15) ----

    [HttpGet("trust-safety")]
    public async Task<IActionResult> GetTrustSafetyDashboard(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Needlr.Application.TrustSafety.GetTrustSafetyDashboard.GetTrustSafetyDashboardQuery(),
            cancellationToken);
        return result.ToActionResult(dto => new Needlr.Contracts.TrustSafety.TrustSafetyDashboardResponse(
            dto.LowFeedbackAverages
                .Select(a => new Needlr.Contracts.TrustSafety.FlaggedArtistResponse(
                    a.ArtistId, a.DisplayName, a.FeedbackCount, a.AverageRating)).ToList(),
            dto.RepeatNotBookingAgain
                .Select(a => new Needlr.Contracts.TrustSafety.FlaggedArtistResponse(
                    a.ArtistId, a.DisplayName, a.FeedbackCount, a.AverageRating)).ToList(),
            dto.SafetyKeywordMatches
                .Select(f => new Needlr.Contracts.TrustSafety.FlaggedFeedbackResponse(
                    f.FeedbackId, f.BookingId, f.ArtistId, f.ArtistDisplayName,
                    f.SubmittedAt, f.MatchedKeyword, f.FreeText)).ToList()));
    }

    [HttpPost("users/{userId:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(
        Guid userId,
        [FromBody] Needlr.Contracts.TrustSafety.SuspendUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Needlr.Application.Moderation.SuspendUser.SuspendUserCommand(userId, request.Reason),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId:guid}/unsuspend")]
    public async Task<IActionResult> UnsuspendUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Needlr.Application.Moderation.UnsuspendUser.UnsuspendUserCommand(userId),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId:guid}/warn")]
    public async Task<IActionResult> WarnUser(
        Guid userId,
        [FromBody] Needlr.Contracts.TrustSafety.WarnUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Needlr.Application.Moderation.WarnUser.WarnUserCommand(userId, request.Reason),
            cancellationToken);
        return result.ToActionResult(id => new Needlr.Contracts.Studios.CreatedIdResponse(id));
    }

    /// <summary>
    /// Paginated admin user search. Email is a substring match (case-insensitive). Role
    /// is exact (Customer / Artist / Admin). Both filters optional.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? email,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        UserRole? parsedRole = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: false, out var r))
                return BadRequest(new { error = "InvalidRole", message = $"Unknown role '{role}'." });
            parsedRole = r;
        }

        var result = await _mediator.Send(new SearchUsersQuery(
            email, parsedRole, new PageRequest(page, pageSize)), cancellationToken);
        return result.ToActionResult(p => new AdminUserPageResponse(
            p.Items.Select(u => new AdminUserResponse(
                u.UserId, u.Email, u.Role.ToString(), u.DisplayName,
                u.CreatedAt, u.SuspendedAt)).ToList(),
            p.Page, p.PageSize, p.TotalCount, p.TotalPages, p.HasPrevious, p.HasNext));
    }

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

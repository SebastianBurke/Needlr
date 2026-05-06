using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Affiliations.ChangeAffiliationRole;
using Needlr.Application.Affiliations.GetMyAffiliations;
using Needlr.Application.Affiliations.InviteArtistToStudio;
using Needlr.Application.Affiliations.ListStudioAffiliations;
using Needlr.Application.Affiliations.RemoveAffiliation;
using Needlr.Application.Affiliations.RequestGuestSpot;
using Needlr.Application.Affiliations.RequestStudioJoin;
using Needlr.Application.Affiliations.RespondToGuestSpot;
using Needlr.Application.Affiliations.RespondToJoinRequest;
using Needlr.Application.Affiliations.RespondToStudioInvitation;
using Needlr.Application.Affiliations.SetPrimaryAffiliation;
using Needlr.Application.Studios;
using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/affiliations")]
[Authorize(Roles = nameof(UserRole.Artist))]
public sealed class AffiliationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyAffiliationsQuery(), cancellationToken);
        return result.ToActionResult(items => items.Select(ToResponse).ToList());
    }

    [HttpPost("join-requests")]
    public async Task<IActionResult> RequestJoin(
        [FromBody] JoinStudioRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RequestStudioJoinCommand(request.StudioId), cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> Invite(
        [FromBody] InviteArtistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new InviteArtistToStudioCommand(request.StudioId, request.ArtistId), cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpPost("guest-spots")]
    public async Task<IActionResult> RequestGuestSpot(
        [FromBody] GuestSpotRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RequestGuestSpotCommand(request.StudioId, request.StartDate, request.EndDate),
            cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    /// <summary>Studio admin approves/rejects a permanent join request.</summary>
    [HttpPost("{id:guid}/admin-decision")]
    public async Task<IActionResult> AdminDecision(
        Guid id,
        [FromBody] AffiliationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RespondToJoinRequestCommand(id, request.Accept), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Invited artist accepts/declines an invitation.</summary>
    [HttpPost("{id:guid}/invitee-decision")]
    public async Task<IActionResult> InviteeDecision(
        Guid id,
        [FromBody] AffiliationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RespondToStudioInvitationCommand(id, request.Accept), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Host studio admin approves/rejects a guest-spot request.</summary>
    [HttpPost("{id:guid}/host-decision")]
    public async Task<IActionResult> HostDecision(
        Guid id,
        [FromBody] AffiliationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RespondToGuestSpotCommand(id, request.Accept), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeAffiliationRoleRequest request,
        CancellationToken cancellationToken)
    {
        var newRole = Enum.TryParse<AffiliationRole>(request.NewRole, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid AffiliationRole: '{request.NewRole}'.");
        var result = await _mediator.Send(new ChangeAffiliationRoleCommand(id, newRole), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveAffiliationCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetPrimaryAffiliationCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Studio-admin roster view — lists every affiliation regardless of status.</summary>
    [HttpGet("by-studio/{studioId:guid}")]
    public async Task<IActionResult> ListByStudio(Guid studioId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListStudioAffiliationsQuery(studioId), cancellationToken);
        return result.ToActionResult(items => items.Select(d => new StudioAffiliationResponse(
            d.AffiliationId, d.ArtistId, d.ArtistDisplayName, d.Role,
            d.AffiliationType, d.Status, d.StartDate, d.EndDate, d.IsPrimary)).ToList());
    }

    private static AffiliationResponse ToResponse(AffiliationDto dto) => new(
        dto.Id,
        dto.ArtistId,
        dto.StudioId,
        dto.StudioName,
        dto.Role.ToString(),
        dto.AffiliationType.ToString(),
        dto.Status.ToString(),
        dto.StartDate,
        dto.EndDate,
        dto.IsPrimary);
}

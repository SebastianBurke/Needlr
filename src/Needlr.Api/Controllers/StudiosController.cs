using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Geography;
using Needlr.Application.Studios;
using Needlr.Application.Studios.CreateStudio;
using Needlr.Application.Studios.GetStudioById;
using Needlr.Application.Studios.GetStudioRoster;
using Needlr.Application.Studios.SearchStudiosByName;
using Needlr.Application.Studios.UpdateStudioInfo;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/studios")]
public sealed class StudiosController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudioRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStudioCommand(
            request.Name,
            ParseEnum<StudioType>(request.StudioType),
            new GeoPoint(request.Location.Latitude, request.Location.Longitude),
            request.Address,
            request.JoinPolicy is null ? null : ParseEnum<JoinPolicy>(request.JoinPolicy),
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "q")] string query,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new SearchStudiosByNameQuery(query, take), cancellationToken);
        return result.ToActionResult(items => items.Select(ToSummary).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStudioByIdQuery(id), cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStudioInfoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStudioInfoCommand(
            id,
            request.Name,
            request.Address,
            request.Description,
            ParseEnum<JoinPolicy>(request.JoinPolicy));
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/roster")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRoster(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStudioRosterQuery(id), cancellationToken);
        return result.ToActionResult(ToRosterResponse);
    }

    private static StudioResponse ToResponse(StudioDto dto) => new(
        dto.Id,
        dto.Name,
        dto.StudioType.ToString(),
        new GeoPointDto(dto.Location.Latitude, dto.Location.Longitude),
        dto.Address,
        dto.JoinPolicy.ToString(),
        dto.Description,
        dto.CreatedByArtistId);

    private static StudioSummaryResponse ToSummary(StudioSummaryDto dto) => new(
        dto.Id, dto.Name, dto.Address, dto.StudioType.ToString(),
        new GeoPointDto(dto.Location.Latitude, dto.Location.Longitude));

    private static StudioRosterResponse ToRosterResponse(StudioRosterDto dto) => new(
        dto.StudioId, dto.StudioName,
        dto.Entries.Select(e => new StudioRosterEntryResponse(
            e.AffiliationId,
            e.ArtistId,
            e.ArtistDisplayName,
            e.Role.ToString(),
            e.AffiliationType.ToString(),
            e.StartDate,
            e.EndDate,
            e.IsPrimary)).ToList());

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

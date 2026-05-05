using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Portfolio;
using Needlr.Application.Portfolio.AddSessionPhoto;
using Needlr.Application.Portfolio.CreatePortfolioPiece;
using Needlr.Application.Portfolio.DeletePortfolioPiece;
using Needlr.Application.Portfolio.GetArtistPortfolio;
using Needlr.Application.Portfolio.GetPortfolioPiece;
using Needlr.Application.Portfolio.GetStudioCollectivePortfolio;
using Needlr.Application.Portfolio.HideSessionPhoto;
using Needlr.Application.Portfolio.UpdatePortfolioPiece;
using Needlr.Application.Portfolio.UploadHealedPhoto;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // ---- Pieces (artist) ----

    [HttpPost("pieces")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> CreatePiece(
        [FromForm] CreatePortfolioPieceRequest meta,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var styleIds = SplitGuids(meta.StyleIds);
        var freeformTags = SplitTags(meta.FreeformTags);

        await using var stream = file.OpenReadStream();
        var command = new CreatePortfolioPieceCommand(
            meta.Title,
            meta.Description,
            ParseEnum<BodyPlacement>(meta.BodyPlacement),
            styleIds,
            freeformTags,
            meta.ApproximateSizeCm,
            meta.EstimatedSessionLengthHours,
            meta.YearCompleted,
            ParseEnum<ProgressionStatus>(meta.ProgressionStatus),
            meta.LinkedBookingId,
            stream,
            file.ContentType,
            file.FileName);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpPatch("pieces/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> UpdatePiece(
        Guid id,
        [FromBody] UpdatePortfolioPieceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePortfolioPieceCommand(
            id,
            request.Title,
            request.Description,
            ParseEnum<BodyPlacement>(request.BodyPlacement),
            request.StyleIds,
            request.FreeformTags,
            request.ApproximateSizeCm,
            request.EstimatedSessionLengthHours,
            request.YearCompleted,
            ParseEnum<ProgressionStatus>(request.ProgressionStatus));

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("pieces/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> DeletePiece(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeletePortfolioPieceCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("pieces/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPiece(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPortfolioPieceQuery(id), cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPost("pieces/{id:guid}/photos")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> AddPhoto(
        Guid id,
        [FromForm] AddSessionPhotoFormRequest meta,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new AddSessionPhotoCommand(
            id,
            ParseEnum<PhotoType>(meta.PhotoType),
            meta.Order,
            meta.LinkedSessionDate is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null,
            stream,
            file.ContentType,
            file.FileName);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(photoId => new CreatedIdResponse(photoId));
    }

    [HttpPost("photos/{photoId:guid}/hide")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> HidePhoto(
        Guid photoId,
        [FromBody] HideSessionPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new HideSessionPhotoCommand(photoId, request.Reason), cancellationToken);
        return result.ToActionResult();
    }

    // ---- Customer-side: healed photo upload ----

    [HttpPost("healed-photos/{bookingId:guid}")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> UploadHealedPhoto(
        Guid bookingId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadHealedPhotoCommand(bookingId, stream, file.ContentType, file.FileName);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(photoId => new CreatedIdResponse(photoId));
    }

    // ---- Listing ----

    [HttpGet("artists/{artistId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetArtistPortfolio(
        Guid artistId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetArtistPortfolioQuery(artistId, new PageRequest(page, pageSize)), cancellationToken);
        return result.ToActionResult(ToPagedResponse);
    }

    [HttpGet("studios/{studioId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStudioCollectivePortfolio(
        Guid studioId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetStudioCollectivePortfolioQuery(studioId, new PageRequest(page, pageSize)), cancellationToken);
        return result.ToActionResult(ToPagedResponse);
    }

    private static PortfolioPieceResponse ToResponse(PortfolioPieceDto dto) => new(
        dto.Id, dto.ArtistId, dto.Title, dto.Description,
        dto.BodyPlacement.ToString(), dto.ApproximateSizeCm, dto.EstimatedSessionLengthHours,
        dto.YearCompleted, dto.ProgressionStatus.ToString(), dto.LinkedBookingId, dto.CreatedAt,
        dto.Styles.Select(s => new TattooStyleResponse(s.Id, s.Name, s.Slug, s.IsCanonical)).ToList(),
        dto.FreeformTags,
        dto.Photos.Select(p => new SessionPhotoResponse(
            p.Id, p.Order, p.PhotoType.ToString(), p.ImageUrl,
            p.UploadedByUserId, p.UploadedByRole.ToString(), p.UploadedAt,
            p.LinkedSessionDate, p.IsHidden, p.HiddenReason)).ToList());

    private static PagedPortfolioResponse ToPagedResponse(PagedResult<PortfolioPieceSummaryDto> page) => new(
        page.Items.Select(p => new PortfolioPieceSummaryResponse(
            p.Id, p.ArtistId, p.Title, p.BodyPlacement.ToString(), p.YearCompleted,
            p.ProgressionStatus.ToString(), p.CreatedAt, p.FreshPhotoUrl, p.HealedPhotoUrl)).ToList(),
        page.Page, page.PageSize, page.TotalCount, page.TotalPages, page.HasPrevious, page.HasNext);

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");

    private static List<Guid> SplitGuids(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g)
                    ? g
                    : throw new ArgumentException($"Invalid guid in list: '{s}'."))
                .ToList();

    private static List<string> SplitTags(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Artists.GetArtistById;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/artists")]
[AllowAnonymous]
public sealed class ArtistsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetArtistByIdQuery(id), cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    private static ArtistDetailResponse ToResponse(ArtistDetailDto dto) => new(
        dto.Id,
        dto.DisplayName,
        dto.Bio,
        dto.YearsExperience,
        dto.HourlyRateCad,
        dto.ShopMinimumCad,
        dto.AcceptingNewBookings,
        dto.PaymentStatus.ToString(),
        dto.CancellationPolicy.ToString(),
        dto.VerificationStatus.ToString(),
        dto.PrimaryStudio is null ? null : new PrimaryStudioSummaryResponse(
            dto.PrimaryStudio.Id,
            dto.PrimaryStudio.Name,
            dto.PrimaryStudio.Address,
            new GeoPointDto(dto.PrimaryStudio.Location.Latitude, dto.PrimaryStudio.Location.Longitude)),
        dto.Styles
            .Select(s => new TattooStyleResponse(s.Id, s.Name, s.Slug, s.IsCanonical))
            .ToList());
}

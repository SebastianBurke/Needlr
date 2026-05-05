using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Discovery;
using Needlr.Application.Discovery.SearchStudios;
using Needlr.Contracts.Discovery;
using Needlr.Contracts.Studios;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/discovery")]
[AllowAnonymous]
public sealed class DiscoveryController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Spatial search for studios. Bounding box is required (the map sets it from the
    /// viewport); style ids and availability dates are optional. Verified filter defaults to
    /// "Verified-only on" — matches FEATURE_SPECS.md § Discovery > Filters.
    /// </summary>
    [HttpGet("studios")]
    public async Task<IActionResult> SearchStudios(
        [FromQuery(Name = "southLat")] double southLat,
        [FromQuery(Name = "westLng")] double westLng,
        [FromQuery(Name = "northLat")] double northLat,
        [FromQuery(Name = "eastLng")] double eastLng,
        [FromQuery(Name = "centerLat")] double centerLat,
        [FromQuery(Name = "centerLng")] double centerLng,
        [FromQuery(Name = "styleIds")] string? styleIds = null,
        [FromQuery(Name = "verifiedOnly")] bool verifiedOnly = true,
        [FromQuery(Name = "availabilityFrom")] DateOnly? availabilityFrom = null,
        [FromQuery(Name = "availabilityTo")] DateOnly? availabilityTo = null,
        [FromQuery(Name = "acceptingNewBookings")] bool acceptingNewBookingsOnly = true,
        [FromQuery] string sort = "DistanceAscending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var parsedStyleIds = ParseGuids(styleIds);
        var parsedSort = Enum.TryParse<DiscoverySort>(sort, ignoreCase: false, out var s)
            ? s
            : throw new ArgumentException(
                $"Invalid sort: '{sort}'. Expected DistanceAscending | AvailabilitySoonness | VerifiedFirst.");

        var criteria = new DiscoverySearchCriteria(
            new BoundingBox(southLat, westLng, northLat, eastLng),
            new GeoPoint(centerLat, centerLng),
            parsedStyleIds,
            verifiedOnly,
            availabilityFrom,
            availabilityTo,
            acceptingNewBookingsOnly,
            parsedSort,
            new PageRequest(page, pageSize));

        var result = await _mediator.Send(new SearchStudiosQuery(criteria), cancellationToken);
        return result.ToActionResult(ToPageResponse);
    }

    private static DiscoveryPageResponse ToPageResponse(PagedResult<DiscoveryStudioDto> page) => new(
        page.Items.Select(d => new DiscoveryStudioResponse(
            d.Id,
            d.Name,
            d.Address,
            d.StudioType.ToString(),
            new GeoPointDto(d.Location.Latitude, d.Location.Longitude),
            d.DistanceFromCenter,
            d.IsVerified,
            d.HasSubmittedDocuments,
            d.ActiveArtistCount)).ToList(),
        page.Page, page.PageSize, page.TotalCount, page.TotalPages, page.HasPrevious, page.HasNext);

    private static List<Guid>? ParseGuids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g)
                ? g
                : throw new ArgumentException($"Invalid guid in styleIds: '{s}'."))
            .ToList();
    }
}

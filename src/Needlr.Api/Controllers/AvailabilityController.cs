using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Availability;
using Needlr.Application.Availability.AddAvailabilityOverride;
using Needlr.Application.Availability.CloseBookingWindow;
using Needlr.Application.Availability.CreateBookingWindow;
using Needlr.Application.Availability.GetArtistProjection;
using Needlr.Application.Availability.GetMyAvailability;
using Needlr.Application.Availability.Ical;
using Needlr.Application.Availability.RebuildArtistAvailabilityProjection;
using Needlr.Application.Availability.RemoveAvailabilityOverride;
using Needlr.Application.Availability.SetAvailabilityPattern;
using Needlr.Application.Availability.SetLeadTimes;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/availability")]
public sealed class AvailabilityController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // ---- Pattern ----

    [HttpPut("pattern")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> SetPattern(
        [FromBody] SetAvailabilityPatternRequest request, CancellationToken cancellationToken)
    {
        var days = request.Days
            .Select(d => new AvailabilityPatternDayInput(
                ParseEnum<DayOfWeek>(d.DayOfWeek),
                ParseEnum<AvailabilityStatus>(d.Status),
                d.MaxSessionHours,
                d.EffectiveFrom,
                d.EffectiveUntil))
            .ToList();
        var result = await _mediator.Send(new SetAvailabilityPatternCommand(days), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("pattern")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> GetPattern(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyAvailabilityPatternQuery(), cancellationToken);
        return result.ToActionResult(rows => new AvailabilityPatternResponse(
            rows.Select(r => new AvailabilityPatternDayResponse(
                r.Id,
                r.DayOfWeek.ToString(),
                r.Status.ToString(),
                r.MaxSessionHours,
                r.EffectiveFrom,
                r.EffectiveUntil)).ToList()));
    }

    // ---- Overrides ----

    [HttpPost("overrides")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> AddOverride(
        [FromBody] AddAvailabilityOverrideRequest request, CancellationToken cancellationToken)
    {
        var command = new AddAvailabilityOverrideCommand(
            request.Date,
            ParseEnum<AvailabilityStatus>(request.Status),
            request.MaxSessionHours,
            request.Reason);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpDelete("overrides/{date}")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> RemoveOverride(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveAvailabilityOverrideCommand(date), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("overrides")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> ListOverrides(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyAvailabilityOverridesQuery(from, to), cancellationToken);
        return result.ToActionResult(rows => new AvailabilityOverridesResponse(
            rows.Select(r => new AvailabilityOverrideResponse(
                r.Id, r.Date, r.Status.ToString(), r.MaxSessionHours, r.Reason)).ToList()));
    }

    // ---- Booking windows ----

    [HttpPost("windows")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> CreateWindow(
        [FromBody] CreateBookingWindowRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBookingWindowCommand(
            EnsureUtc(request.WindowOpensAt),
            EnsureUtc(request.WindowClosesAt),
            request.TargetRangeStart,
            request.TargetRangeEnd);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpDelete("windows/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> CloseWindow(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CloseBookingWindowCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("windows")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> ListWindows(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyBookingWindowsQuery(), cancellationToken);
        return result.ToActionResult(rows => new BookingWindowsResponse(
            rows.Select(w => new BookingWindowResponse(
                w.Id, w.WindowOpensAt, w.WindowClosesAt, w.TargetRangeStart, w.TargetRangeEnd)).ToList()));
    }

    // ---- Lead times ----

    [HttpPut("lead-times")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> SetLeadTimes(
        [FromBody] SetLeadTimesRequest request, CancellationToken cancellationToken)
    {
        var items = request.LeadTimes
            .Select(lt => new LeadTimeDto(ParseEnum<BookingType>(lt.BookingType), lt.MinimumDays))
            .ToList();
        var result = await _mediator.Send(new SetLeadTimesCommand(items), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("lead-times")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> ListLeadTimes(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyLeadTimesQuery(), cancellationToken);
        return result.ToActionResult(rows => new LeadTimesResponse(
            rows.Select(r => new LeadTimeResponseItem(r.BookingType.ToString(), r.MinimumDays)).ToList()));
    }

    // ---- Projection ----

    /// <summary>Public read of an artist's precomputed availability for a date range.</summary>
    [HttpGet("artists/{artistId:guid}/projection")]
    [AllowAnonymous]
    public async Task<IActionResult> GetArtistProjection(
        Guid artistId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetArtistProjectionQuery(artistId, from, to), cancellationToken);
        return result.ToActionResult(rows => new ProjectionResponse(
            rows.Select(p => new ProjectionDayResponse(p.Date, p.IsBookable, p.RemainingSessionHours)).ToList()));
    }

    [HttpPost("projection/rebuild/{artistId:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> RebuildArtistProjection(Guid artistId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RebuildArtistAvailabilityProjectionCommand(artistId), cancellationToken);
        return result.ToActionResult();
    }

    // ---- iCal feed ----

    [HttpPost("ical/rotate")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> RotateIcalToken(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RotateIcalTokenCommand(), cancellationToken);
        return result.ToActionResult(rotated =>
        {
            var feedUrl = $"{Request.Scheme}://{Request.Host}/api/availability/ical/{rotated.ArtistId}/{rotated.Token}.ics";
            return new IcalFeedResponse(rotated.Token, feedUrl);
        });
    }

    [HttpGet("ical/{artistId:guid}/{token}.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIcalFeed(
        Guid artistId, string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIcalFeedQuery(artistId, token), cancellationToken);
        if (result.IsFailure)
            return result.ToActionResult(_ => (object)null!);
        return new ContentResult
        {
            ContentType = "text/calendar; charset=utf-8",
            Content = result.Value!,
            StatusCode = StatusCodes.Status200OK
        };
    }

    // ---- helpers ----

    private static DateTime EnsureUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

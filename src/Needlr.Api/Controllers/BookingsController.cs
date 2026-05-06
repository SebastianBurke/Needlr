using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Bookings;
using Needlr.Application.Bookings.AcceptBooking;
using Needlr.Application.Bookings.CancelBookingByArtist;
using Needlr.Application.Bookings.CancelBookingByCustomer;
using Needlr.Application.Bookings.DeclineBooking;
using Needlr.Application.Bookings.GetBookingDetail;
using Needlr.Application.Bookings.GetMyBookingsAsArtist;
using Needlr.Application.Bookings.GetMyBookingsAsCustomer;
using Needlr.Application.Bookings.MarkBookingCompleted;
using Needlr.Application.Bookings.MarkBookingInProgress;
using Needlr.Application.Bookings.RequestBooking;
using Needlr.Application.Bookings.RequestMoreInfo;
using Needlr.Application.Bookings.RespondWithMoreInfo;
using Needlr.Application.Common.Pagination;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    // ---- Customer-initiated ----

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> Submit(
        [FromBody] RequestBookingRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestBookingCommand(
            request.ArtistId,
            ParseEnum<BookingType>(request.BookingType),
            request.RequestedDate,
            request.EstimatedDurationHours,
            request.Description,
            ParseEnum<BodyPlacement>(request.BodyPlacement),
            request.CustomerPaymentMethodId,
            request.ApproximateSizeCm,
            request.EstimatedTotalCad);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpPost("{id:guid}/respond-info")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> RespondInfo(
        Guid id, [FromBody] RespondWithMoreInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new RespondWithMoreInfoCommand(
            id,
            request.Description,
            request.RequestedDate,
            request.EstimatedDurationHours,
            ParseEnum<BodyPlacement>(request.BodyPlacement),
            request.ApproximateSizeCm,
            request.EstimatedTotalCad);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/cancel-customer")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CancelByCustomer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelBookingByCustomerCommand(id), cancellationToken);
        return result.ToActionResult(r => new CancelBookingResponse(r.RefundedAmountCad));
    }

    // ---- Artist-initiated ----

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> Accept(
        Guid id, [FromBody] AcceptBookingRequest request, CancellationToken cancellationToken)
    {
        var command = new AcceptBookingCommand(id, EnsureUtc(request.ConfirmedSessionDateUtc));
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/decline")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> Decline(
        Guid id, [FromBody] DeclineBookingRequest request, CancellationToken cancellationToken)
    {
        var command = new DeclineBookingCommand(
            id, ParseEnum<DeclineReason>(request.Reason), request.Note);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/request-info")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> RequestInfo(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RequestMoreInfoCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/in-progress")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> MarkInProgress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkBookingInProgressCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkBookingCompletedCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/cancel-artist")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> CancelByArtist(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelBookingByArtistCommand(id), cancellationToken);
        return result.ToActionResult(r => new CancelBookingResponse(r.RefundedAmountCad));
    }

    // ---- Reads ----

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBookingDetailQuery(id), cancellationToken);
        return result.ToActionResult(ToDetailResponse);
    }

    [HttpGet("mine/customer")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> ListMineAsCustomer(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyBookingsAsCustomerQuery(
            status is null ? null : ParseEnum<BookingStatus>(status), page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult(ToPageResponse);
    }

    [HttpGet("mine/artist")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> ListMineAsArtist(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyBookingsAsArtistQuery(
            status is null ? null : ParseEnum<BookingStatus>(status), page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult(ToPageResponse);
    }

    // ---- helpers ----

    private static BookingDetailResponse ToDetailResponse(BookingDetailDto d) => new(
        d.Id,
        d.CustomerId,
        d.ArtistId,
        d.StudioId,
        d.BookingType.ToString(),
        d.Status.ToString(),
        d.RequestedAt,
        d.RequestedDate,
        d.EstimatedDurationHours,
        d.Description,
        d.BodyPlacement.ToString(),
        d.ApproximateSizeCm,
        d.EstimatedTotalCad,
        d.DepositAmountCad,
        d.AcceptedAt,
        d.ConfirmedSessionDate,
        d.CompletedAt,
        d.DepositCapturedAt,
        d.CancellationPolicySnapshot.ToString(),
        d.DeclineReason?.ToString(),
        d.DeclineNote);

    private static BookingPageResponse ToPageResponse(PagedResult<BookingSummaryDto> p) => new(
        p.Items.Select(s => new BookingSummaryResponse(
            s.Id, s.CustomerId, s.ArtistId,
            s.BookingType.ToString(), s.Status.ToString(),
            s.RequestedAt, s.RequestedDate, s.ConfirmedSessionDate)).ToList(),
        p.Page,
        p.PageSize,
        p.TotalCount,
        p.HasNext,
        p.HasPrevious);

    private static DateTime EnsureUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

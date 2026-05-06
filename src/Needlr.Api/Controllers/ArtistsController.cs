using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Artists.GetArtistById;
using Needlr.Application.Artists.GetMyArtist;
using Needlr.Application.Artists.SetAcceptingBookings;
using Needlr.Application.Artists.UpdateArtistProfile;
using Needlr.Application.Stripe.CreateConnectAccount;
using Needlr.Application.Stripe.GenerateOnboardingLink;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/artists")]
public sealed class ArtistsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetArtistByIdQuery(id), cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    /// <summary>Returns the calling artist's full profile (drives the settings form).</summary>
    [HttpGet("me")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyArtistQuery(), cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    /// <summary>
    /// Creates the calling artist's Stripe Express Connect account. Idempotent — re-calls
    /// reuse the existing account id.
    /// </summary>
    [HttpPost("me/connect-account")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> CreateConnectAccount(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateConnectAccountCommand(), cancellationToken);
        return result.ToActionResult(id => new ConnectAccountResponse(id));
    }

    /// <summary>Returns a fresh hosted Stripe onboarding URL for the calling artist.</summary>
    [HttpPost("me/onboarding-link")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> GenerateOnboardingLink(
        [FromBody] OnboardingLinkRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateOnboardingLinkCommand(request?.ReturnUrl, request?.RefreshUrl),
            cancellationToken);
        return result.ToActionResult(url => new OnboardingLinkResponse(url));
    }

    /// <summary>
    /// Reads the calling artist's accepting-new-bookings flag — backs the settings form.
    /// </summary>
    [HttpGet("me/accepting-bookings")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> GetMyAcceptingBookings(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyAcceptingBookingsQuery(), cancellationToken);
        return result.ToActionResult(accepting => new SetAcceptingBookingsRequest(accepting));
    }

    /// <summary>
    /// Toggles whether the calling artist accepts new booking requests. Paused artists
    /// stay visible in discovery + on studio rosters; the FE just renders a "not taking
    /// bookings" indicator on the profile.
    /// </summary>
    [HttpPut("me/accepting-bookings")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> SetAcceptingBookings(
        [FromBody] SetAcceptingBookingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetAcceptingBookingsCommand(request.Accepting), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Updates the calling artist's editable profile fields (bio, hourly rate, shop minimum,
    /// years experience, cancellation policy).
    /// </summary>
    [HttpPatch("me")]
    [Authorize(Roles = nameof(UserRole.Artist))]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateArtistProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CancellationPolicy>(request.CancellationPolicy, ignoreCase: false, out var policy))
            return BadRequest(new { error = "InvalidCancellationPolicy", message = $"Unknown cancellation policy '{request.CancellationPolicy}'." });

        var result = await _mediator.Send(new UpdateArtistProfileCommand(
            request.Bio,
            request.YearsExperience,
            request.HourlyRateCad,
            request.ShopMinimumCad,
            policy), cancellationToken);
        return result.ToActionResult();
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
            .ToList(),
        new Needlr.Contracts.TrustSafety.BehavioralSignalsResponse(
            dto.BehavioralSignals.ResponseMedianHours,
            dto.BehavioralSignals.CompletionRatePercent,
            dto.BehavioralSignals.HealedPhotoRatePercent,
            dto.BehavioralSignals.RepeatClientRatePercent));
}

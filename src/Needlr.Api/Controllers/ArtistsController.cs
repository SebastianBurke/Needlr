using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Artists.GetArtistById;
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

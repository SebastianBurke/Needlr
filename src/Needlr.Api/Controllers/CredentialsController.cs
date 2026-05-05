using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Verification.UploadArtistCredential;
using Needlr.Application.Verification.UploadStudioCredential;
using Needlr.Contracts.Studios;
using Needlr.Contracts.Verification;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/credentials")]
[Authorize(Roles = nameof(UserRole.Artist))]
public sealed class CredentialsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>Studio admin uploads a studio-level credential (RSSS, municipal reg., etc.).</summary>
    [HttpPost("studios/{studioId:guid}")]
    public async Task<IActionResult> UploadStudioCredential(
        Guid studioId,
        [FromForm] UploadCredentialRequest meta,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadStudioCredentialCommand(
            studioId,
            meta.JurisdictionId,
            ParseEnum<StudioCredentialType>(meta.CredentialType),
            meta.IssuedDate,
            meta.ExpiryDate,
            stream,
            file.ContentType,
            file.FileName);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    /// <summary>Artist uploads one of their own credentials (bloodborne cert, hygiene training, etc.).</summary>
    [HttpPost("artists/me")]
    public async Task<IActionResult> UploadOwnArtistCredential(
        [FromForm] UploadCredentialRequest meta,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadArtistCredentialCommand(
            meta.JurisdictionId,
            ParseEnum<ArtistCredentialType>(meta.CredentialType),
            meta.IssuedDate,
            meta.ExpiryDate,
            stream,
            file.ContentType,
            file.FileName);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

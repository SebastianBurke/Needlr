using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Auth;
using Needlr.Application.Auth.Login;
using Needlr.Application.Auth.Logout;
using Needlr.Application.Auth.RefreshToken;
using Needlr.Application.Auth.RegisterArtist;
using Needlr.Application.Auth.RegisterCustomer;
using Needlr.Contracts.Auth;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterCustomerCommand(request.Email, request.Password, request.DisplayName),
            cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPost("register-artist")]
    public async Task<IActionResult> RegisterArtist(
        [FromBody] RegisterArtistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterArtistCommand(request.Email, request.Password, request.DisplayName, request.YearsExperience),
            cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);
        return result.ToActionResult(ToResponse);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new LogoutCommand(request.RefreshToken),
            cancellationToken);
        return result.ToActionResult();
    }

    private static AuthResponse ToResponse(AuthResult auth) => new(
        auth.UserId,
        auth.Email,
        auth.Role.ToString(),
        auth.AccessToken,
        auth.AccessTokenExpiresAtUtc,
        auth.RefreshToken,
        auth.RefreshTokenExpiresAtUtc);
}

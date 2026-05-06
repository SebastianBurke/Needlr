using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Customers.GetMyProfile;
using Needlr.Application.Customers.UpdateMyProfile;
using Needlr.Contracts.Customers;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

/// <summary>
/// Customer-self endpoints. The calling customer's editable profile lives here; admin-side
/// customer reads/writes go through the admin controllers.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = nameof(UserRole.Customer))]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    /// <summary>Returns the calling customer's profile (display name + email).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyProfileQuery(), cancellationToken);
        return result.ToActionResult(dto =>
            new MyCustomerProfileResponse(dto.Id, dto.DisplayName, dto.Email));
    }

    /// <summary>Updates the calling customer's editable fields. v1: display name only.</summary>
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateMyCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateMyProfileCommand(request.DisplayName), cancellationToken);
        return result.ToActionResult();
    }
}

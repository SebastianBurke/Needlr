using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Api.Common;
using Needlr.Application.Notifications;
using Needlr.Application.Notifications.GetMyNotificationPreferences;
using Needlr.Application.Notifications.RegisterPushSubscription;
using Needlr.Application.Notifications.UnregisterPushSubscription;
using Needlr.Application.Notifications.UpdateNotificationPreferences;
using Needlr.Contracts.Notifications;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyNotificationPreferencesQuery(), cancellationToken);
        return result.ToActionResult(prefs => new NotificationPreferencesResponse(
            prefs.Select(p => new NotificationPreferenceResponse(
                p.Type.ToString(), p.EmailEnabled, p.PushEnabled)).ToList()));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken)
    {
        var items = request.Preferences
            .Select(p => new NotificationPreferenceDto(
                ParseEnum<NotificationType>(p.Type), p.EmailEnabled, p.PushEnabled))
            .ToList();
        var result = await _mediator.Send(new UpdateNotificationPreferencesCommand(items), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("push-subscriptions")]
    public async Task<IActionResult> RegisterPushSubscription(
        [FromBody] RegisterPushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterPushSubscriptionCommand(
            request.Endpoint, request.P256dh, request.Auth), cancellationToken);
        return result.ToActionResult(id => new CreatedIdResponse(id));
    }

    [HttpDelete("push-subscriptions/{id:guid}")]
    public async Task<IActionResult> UnregisterPushSubscription(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UnregisterPushSubscriptionCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    private static T ParseEnum<T>(string raw) where T : struct, Enum =>
        Enum.TryParse<T>(raw, ignoreCase: false, out var parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name}: '{raw}'.");
}

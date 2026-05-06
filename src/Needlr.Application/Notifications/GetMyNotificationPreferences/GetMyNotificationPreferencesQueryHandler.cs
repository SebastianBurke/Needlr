using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Notifications.GetMyNotificationPreferences;

internal sealed class GetMyNotificationPreferencesQueryHandler(
    ICurrentUser currentUser,
    INotificationPreferenceRepository preferences)
    : IRequestHandler<GetMyNotificationPreferencesQuery, Result<IReadOnlyList<NotificationPreferenceDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(
        GetMyNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<IReadOnlyList<NotificationPreferenceDto>>.Failure(Error.Unauthorized());

        var stored = await preferences.ListByUserAsync(currentUser.UserId.Value, cancellationToken);
        var byType = stored.ToDictionary(p => p.Type);

        IReadOnlyList<NotificationPreferenceDto> result = Enum.GetValues<NotificationType>()
            .Select(t => byType.TryGetValue(t, out var p)
                ? new NotificationPreferenceDto(t, p.EmailEnabled, p.PushEnabled)
                : new NotificationPreferenceDto(t, EmailEnabled: true, PushEnabled: true))
            .ToList();
        return Result<IReadOnlyList<NotificationPreferenceDto>>.Success(result);
    }
}

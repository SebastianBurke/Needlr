using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Notifications;

namespace Needlr.Application.Notifications.UpdateNotificationPreferences;

internal sealed class UpdateNotificationPreferencesCommandHandler(
    ICurrentUser currentUser,
    INotificationPreferenceRepository preferences) : IRequestHandler<UpdateNotificationPreferencesCommand, Result>
{
    public async Task<Result> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        // Disallow same-type duplicates in a single request — keeps the upsert deterministic.
        if (request.Preferences.GroupBy(p => p.Type).Any(g => g.Count() > 1))
            return Result.Failure(Error.Validation("Duplicate NotificationType in request."));

        foreach (var input in request.Preferences)
        {
            var existing = await preferences.GetAsync(userId, input.Type, cancellationToken);
            if (existing is null)
            {
                preferences.Add(new NotificationPreference(
                    id: Guid.NewGuid(),
                    userId: userId,
                    type: input.Type,
                    emailEnabled: input.EmailEnabled,
                    pushEnabled: input.PushEnabled));
            }
            else
            {
                existing.EmailEnabled = input.EmailEnabled;
                existing.PushEnabled = input.PushEnabled;
            }
        }

        return Result.Success();
    }
}

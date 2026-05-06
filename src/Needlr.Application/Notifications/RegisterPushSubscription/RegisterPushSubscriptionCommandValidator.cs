using FluentValidation;
using Needlr.Domain.Notifications;

namespace Needlr.Application.Notifications.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionCommandValidator
    : AbstractValidator<RegisterPushSubscriptionCommand>
{
    public RegisterPushSubscriptionCommandValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(PushSubscription.EndpointMaxLength);
        RuleFor(x => x.P256dh).NotEmpty().MaximumLength(PushSubscription.P256dhMaxLength);
        RuleFor(x => x.Auth).NotEmpty().MaximumLength(PushSubscription.AuthMaxLength);
    }
}

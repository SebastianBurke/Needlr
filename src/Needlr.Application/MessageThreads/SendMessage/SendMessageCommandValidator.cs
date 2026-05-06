using FluentValidation;
using Needlr.Domain.Messaging;

namespace Needlr.Application.MessageThreads.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(Message.BodyMaxLength);
    }
}

using FluentValidation;
using Needlr.Domain.Messaging;

namespace Needlr.Application.MessageThreads.ReportMessage;

public sealed class ReportMessageCommandValidator : AbstractValidator<ReportMessageCommand>
{
    public ReportMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Note)
            .MaximumLength(MessageReport.NoteMaxLength)
            .When(x => x.Note is not null);
    }
}

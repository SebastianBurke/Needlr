using FluentValidation;

namespace Needlr.Application.Affiliations.RequestStudioJoin;

public sealed class RequestStudioJoinCommandValidator : AbstractValidator<RequestStudioJoinCommand>
{
    public RequestStudioJoinCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
    }
}

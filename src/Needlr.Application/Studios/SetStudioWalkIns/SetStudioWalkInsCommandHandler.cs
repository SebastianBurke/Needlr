using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Studios.SetStudioWalkIns;

internal sealed class SetStudioWalkInsCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios) : IRequestHandler<SetStudioWalkInsCommand, Result>
{
    public async Task<Result> Handle(SetStudioWalkInsCommand request, CancellationToken cancellationToken)
    {
        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(request.StudioId, cancellationToken))
            return Result.Failure(Error.Forbidden("You must be an admin of this studio."));

        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result.Failure(Error.NotFound("Studio"));

        studio.AcceptsWalkIns = request.AcceptsWalkIns;
        // Save handled by TransactionBehavior pipeline.
        return Result.Success();
    }
}

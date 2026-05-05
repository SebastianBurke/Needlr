using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Studios.UpdateStudioInfo;

internal sealed class UpdateStudioInfoCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios) : IRequestHandler<UpdateStudioInfoCommand, Result>
{
    public async Task<Result> Handle(UpdateStudioInfoCommand request, CancellationToken cancellationToken)
    {
        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(request.StudioId, cancellationToken))
            return Result.Failure(Error.Forbidden("You must be an admin of this studio."));

        var studio = await studios.GetByIdAsync(request.StudioId, cancellationToken);
        if (studio is null)
            return Result.Failure(Error.NotFound("Studio"));

        studio.Name = request.Name.Trim();
        studio.Address = request.Address.Trim();
        studio.Description = request.Description?.Trim();
        studio.JoinPolicy = request.JoinPolicy;

        return Result.Success();
    }
}

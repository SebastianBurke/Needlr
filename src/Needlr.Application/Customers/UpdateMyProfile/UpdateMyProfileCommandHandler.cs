using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Customers.UpdateMyProfile;

internal sealed class UpdateMyProfileCommandHandler(
    ICurrentUser currentUser,
    ICustomerProfileRepository customers)
    : IRequestHandler<UpdateMyProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure(Error.Forbidden("Authentication required."));

        var profile = await customers.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure(Error.NotFound("CustomerProfile"));

        profile.DisplayName = request.DisplayName.Trim();
        // Save handled by TransactionBehavior pipeline.
        return Result.Success();
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Customers.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(
    ICurrentUser currentUser,
    ICustomerProfileRepository customers)
    : IRequestHandler<GetMyProfileQuery, Result<MyCustomerProfileDto>>
{
    public async Task<Result<MyCustomerProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return Result<MyCustomerProfileDto>.Failure(Error.Forbidden("Authentication required."));

        var profile = await customers.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<MyCustomerProfileDto>.Failure(Error.NotFound("CustomerProfile"));

        return Result<MyCustomerProfileDto>.Success(new MyCustomerProfileDto(
            profile.Id,
            profile.DisplayName,
            currentUser.Email));
    }
}

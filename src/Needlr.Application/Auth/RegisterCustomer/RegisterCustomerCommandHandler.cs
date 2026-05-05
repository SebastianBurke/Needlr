using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Auth.RegisterCustomer;

internal sealed class RegisterCustomerCommandHandler(
    IUserAccountService userAccountService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore)
    : IRequestHandler<RegisterCustomerCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var registration = await userAccountService.RegisterCustomerAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            cancellationToken);

        if (!registration.Succeeded)
        {
            // Collapse Identity's "DuplicateUserName"/"DuplicateEmail" into a Conflict so the API
            // can return 409; everything else surfaces as a generic Validation error. The Identity
            // error codes vary across versions, but messages are stable enough for the test suite
            // to target.
            return Result<AuthResult>.Failure(registration.Errors.Select(MapIdentityError));
        }

        var accessToken = jwtTokenService.Issue(
            registration.UserId,
            request.Email,
            UserRole.Customer);

        var refreshToken = await refreshTokenStore.IssueAsync(registration.UserId, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult(
            registration.UserId,
            request.Email,
            UserRole.Customer,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc));
    }

    private static Error MapIdentityError(string description)
    {
        if (description.Contains("already taken", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            return Error.Conflict(description);
        return Error.Validation(description);
    }
}

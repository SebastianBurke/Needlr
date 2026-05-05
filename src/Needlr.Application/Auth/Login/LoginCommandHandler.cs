using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Auth.Login;

internal sealed class LoginCommandHandler(
    IUserAccountService userAccountService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore)
    : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userAccountService.CheckCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            // Single error for both unknown-email and wrong-password to avoid user enumeration.
            return Result<AuthResult>.Failure(Error.Unauthorized("Invalid email or password."));
        }

        var accessToken = jwtTokenService.Issue(user.UserId, user.Email, user.Role);
        var refreshToken = await refreshTokenStore.IssueAsync(user.UserId, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult(
            user.UserId,
            user.Email,
            user.Role,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc));
    }
}

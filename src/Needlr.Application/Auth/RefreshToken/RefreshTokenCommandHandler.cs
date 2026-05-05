using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenStore refreshTokenStore,
    IUserAccountService userAccountService,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var rotated = await refreshTokenStore.RotateAsync(request.RefreshToken, cancellationToken);
        if (rotated is null)
            return Result<AuthResult>.Failure(Error.Unauthorized("Refresh token is invalid, expired, or already used."));

        var user = await userAccountService.FindByIdAsync(rotated.UserId, cancellationToken);
        if (user is null)
        {
            // Token rotated to a now-deleted user — shouldn't happen, but treat as unauthorized.
            return Result<AuthResult>.Failure(Error.Unauthorized("Refresh token is invalid, expired, or already used."));
        }

        var accessToken = jwtTokenService.Issue(user.UserId, user.Email, user.Role);

        return Result<AuthResult>.Success(new AuthResult(
            user.UserId,
            user.Email,
            user.Role,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            rotated.Token,
            rotated.ExpiresAtUtc));
    }
}

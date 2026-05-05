using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Auth.RegisterArtist;

internal sealed class RegisterArtistCommandHandler(
    IUserAccountService userAccountService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenStore refreshTokenStore)
    : IRequestHandler<RegisterArtistCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(RegisterArtistCommand request, CancellationToken cancellationToken)
    {
        var registration = await userAccountService.RegisterArtistAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.YearsExperience,
            cancellationToken);

        if (!registration.Succeeded)
            return Result<AuthResult>.Failure(registration.Errors.Select(MapIdentityError));

        var accessToken = jwtTokenService.Issue(
            registration.UserId,
            request.Email,
            UserRole.Artist);

        var refreshToken = await refreshTokenStore.IssueAsync(registration.UserId, cancellationToken);

        return Result<AuthResult>.Success(new AuthResult(
            registration.UserId,
            request.Email,
            UserRole.Artist,
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

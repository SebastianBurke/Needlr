using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Auth.Logout;

internal sealed class LogoutCommandHandler(IRefreshTokenStore refreshTokenStore)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: revoking an unknown or already-revoked token is a no-op (don't leak token state).
        await refreshTokenStore.RevokeAsync(request.RefreshToken, cancellationToken);
        return Result.Success();
    }
}

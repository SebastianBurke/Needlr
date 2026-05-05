using Needlr.Application.Messaging;

namespace Needlr.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResult>;

using Needlr.Application.Messaging;

namespace Needlr.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResult>;

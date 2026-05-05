using Needlr.Application.Messaging;

namespace Needlr.Application.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;

using Needlr.Application.Messaging;

namespace Needlr.Application.Moderation.UnsuspendUser;

public sealed record UnsuspendUserCommand(Guid UserId) : ICommand;

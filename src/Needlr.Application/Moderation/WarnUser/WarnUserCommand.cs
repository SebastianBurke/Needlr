using Needlr.Application.Messaging;

namespace Needlr.Application.Moderation.WarnUser;

/// <summary>
/// Records an admin warning for a user. Doesn't change the user's runtime status — purely
/// audit / customer-service signal. Repeated warnings against the same user are allowed
/// (each one is its own row).
/// </summary>
public sealed record WarnUserCommand(Guid UserId, string Reason) : ICommand<Guid>;

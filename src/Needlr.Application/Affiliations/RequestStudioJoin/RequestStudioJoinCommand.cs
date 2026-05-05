using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RequestStudioJoin;

public sealed record RequestStudioJoinCommand(Guid StudioId) : ICommand<Guid>;

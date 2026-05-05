using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RespondToJoinRequest;

public sealed record RespondToJoinRequestCommand(Guid AffiliationId, bool Accept) : ICommand;

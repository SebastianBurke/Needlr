using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RespondToStudioInvitation;

public sealed record RespondToStudioInvitationCommand(Guid AffiliationId, bool Accept) : ICommand;

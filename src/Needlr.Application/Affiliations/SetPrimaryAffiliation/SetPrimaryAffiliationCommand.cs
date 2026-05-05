using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.SetPrimaryAffiliation;

public sealed record SetPrimaryAffiliationCommand(Guid AffiliationId) : ICommand;

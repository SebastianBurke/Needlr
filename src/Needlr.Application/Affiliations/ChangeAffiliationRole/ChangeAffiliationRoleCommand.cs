using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Affiliations.ChangeAffiliationRole;

public sealed record ChangeAffiliationRoleCommand(Guid AffiliationId, AffiliationRole NewRole) : ICommand;

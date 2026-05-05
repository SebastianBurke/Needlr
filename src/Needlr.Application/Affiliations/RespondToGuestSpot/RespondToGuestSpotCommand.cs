using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RespondToGuestSpot;

public sealed record RespondToGuestSpotCommand(Guid AffiliationId, bool Accept) : ICommand;

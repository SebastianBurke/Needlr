using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RemoveAffiliation;

/// <summary>Removes (ends) an affiliation. Both a studio admin and the affiliated artist
/// themselves can call this; founders cannot leave without first ceding founder status.</summary>
public sealed record RemoveAffiliationCommand(Guid AffiliationId) : ICommand;

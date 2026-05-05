using Needlr.Application.Messaging;
using Needlr.Application.Studios;

namespace Needlr.Application.Affiliations.GetMyAffiliations;

public sealed record GetMyAffiliationsQuery() : IQuery<IReadOnlyList<AffiliationDto>>;

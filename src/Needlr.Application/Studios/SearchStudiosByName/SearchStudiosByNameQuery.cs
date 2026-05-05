using Needlr.Application.Messaging;

namespace Needlr.Application.Studios.SearchStudiosByName;

public sealed record SearchStudiosByNameQuery(string Query, int Take = 20)
    : IQuery<IReadOnlyList<StudioSummaryDto>>;

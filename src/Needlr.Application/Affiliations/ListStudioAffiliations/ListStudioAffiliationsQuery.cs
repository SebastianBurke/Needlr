using MediatR;
using Needlr.Application.Common.Results;
using Needlr.Application.Studios;

namespace Needlr.Application.Affiliations.ListStudioAffiliations;

/// <summary>
/// Admin-side roster view: lists every affiliation on a studio (Pending + Active + Ended +
/// Rejected) with artist display name. Caller must be a studio admin (Founder/Admin role).
/// Backs the studio-admin roster page.
/// </summary>
public sealed record ListStudioAffiliationsQuery(Guid StudioId)
    : IRequest<Result<IReadOnlyList<StudioAffiliationDetailDto>>>;

/// <summary>
/// Per-row roster entry — like <see cref="StudioRosterEntryDto"/> but includes Status (so
/// pending entries are visible) and is unfiltered.
/// </summary>
public sealed record StudioAffiliationDetailDto(
    Guid AffiliationId,
    Guid ArtistId,
    string ArtistDisplayName,
    string Role,
    string AffiliationType,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary);

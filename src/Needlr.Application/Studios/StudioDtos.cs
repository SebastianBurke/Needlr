using Needlr.Application.Common.Geography;
using Needlr.Domain.Enums;

namespace Needlr.Application.Studios;

/// <summary>Studio detail row used by GetStudioById.</summary>
public sealed record StudioDto(
    Guid Id,
    string Name,
    StudioType StudioType,
    GeoPoint Location,
    string Address,
    JoinPolicy JoinPolicy,
    string? Description,
    Guid CreatedByArtistId);

/// <summary>Lightweight row for search / list results.</summary>
public sealed record StudioSummaryDto(
    Guid Id,
    string Name,
    string Address,
    StudioType StudioType,
    GeoPoint Location);

/// <summary>One row of a studio's roster. <see cref="AcceptingNewBookings"/> reflects the
/// artist's pause toggle — paused artists still appear here so customers can find them and
/// check back later, but the FE renders a "not taking bookings" indicator.</summary>
public sealed record StudioRosterEntryDto(
    Guid AffiliationId,
    Guid ArtistId,
    string ArtistDisplayName,
    AffiliationRole Role,
    AffiliationType AffiliationType,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary,
    bool AcceptingNewBookings);

public sealed record StudioRosterDto(
    Guid StudioId,
    string StudioName,
    IReadOnlyList<StudioRosterEntryDto> Entries);

/// <summary>The current user's view of an affiliation.</summary>
public sealed record AffiliationDto(
    Guid Id,
    Guid ArtistId,
    Guid StudioId,
    string StudioName,
    AffiliationRole Role,
    AffiliationType AffiliationType,
    AffiliationStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary);

using Needlr.Contracts.Common;

namespace Needlr.Contracts.Studios;

// ---- Requests ----

public sealed record CreateStudioRequest(
    string Name,
    string StudioType,        // wire-format string of Domain.Enums.StudioType
    GeoPointDto Location,
    string Address,
    string? JoinPolicy = null, // optional, server defaults via Studio.DefaultJoinPolicyFor
    string? Description = null);

public sealed record UpdateStudioInfoRequest(
    string Name,
    string Address,
    string? Description,
    string JoinPolicy);

// ---- Responses ----

public sealed record GeoPointDto(double Latitude, double Longitude);

public sealed record StudioResponse(
    Guid Id,
    string Name,
    string StudioType,
    GeoPointDto Location,
    string Address,
    string JoinPolicy,
    string? Description,
    Guid CreatedByArtistId);

public sealed record StudioSummaryResponse(
    Guid Id,
    string Name,
    string Address,
    string StudioType,
    GeoPointDto Location);

public sealed record StudioRosterEntryResponse(
    Guid AffiliationId,
    Guid ArtistId,
    string ArtistDisplayName,
    string Role,
    string AffiliationType,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary,
    bool AcceptingNewBookings);

public sealed record StudioRosterResponse(
    Guid StudioId,
    string StudioName,
    IReadOnlyList<StudioRosterEntryResponse> Entries);

public sealed record CreatedIdResponse(Guid Id);

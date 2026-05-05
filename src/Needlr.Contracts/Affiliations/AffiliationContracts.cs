namespace Needlr.Contracts.Affiliations;

// ---- Requests ----

public sealed record JoinStudioRequest(Guid StudioId);

public sealed record InviteArtistRequest(Guid StudioId, Guid ArtistId);

public sealed record GuestSpotRequest(Guid StudioId, DateOnly StartDate, DateOnly EndDate);

public sealed record AffiliationDecisionRequest(bool Accept);

public sealed record ChangeAffiliationRoleRequest(string NewRole);

// ---- Responses ----

public sealed record AffiliationResponse(
    Guid Id,
    Guid ArtistId,
    Guid StudioId,
    string StudioName,
    string Role,
    string AffiliationType,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary);

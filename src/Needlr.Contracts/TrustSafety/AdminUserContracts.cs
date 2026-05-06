namespace Needlr.Contracts.TrustSafety;

/// <summary>One row of the admin user-search result.</summary>
public sealed record AdminUserResponse(
    Guid UserId,
    string Email,
    string Role,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime? SuspendedAt);

public sealed record AdminUserPageResponse(
    IReadOnlyList<AdminUserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

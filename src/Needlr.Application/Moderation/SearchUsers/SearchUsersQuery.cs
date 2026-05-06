using MediatR;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Moderation.SearchUsers;

/// <summary>
/// Admin-only paginated search across all users. <see cref="EmailSubstring"/> matches
/// case-insensitively; <see cref="Role"/> is exact match. Both filters are optional.
/// </summary>
public sealed record SearchUsersQuery(
    string? EmailSubstring,
    UserRole? Role,
    PageRequest Page) : IRequest<Result<PagedResult<AdminUserDto>>>;

/// <summary>
/// One row of the admin user-search result. Display name is best-effort: filled in from
/// <c>CustomerProfile.DisplayName</c> for customers and <c>Artist.DisplayName</c> for
/// artists; admins have no profile and surface as just their email.
/// </summary>
public sealed record AdminUserDto(
    Guid UserId,
    string Email,
    UserRole Role,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime? SuspendedAt);

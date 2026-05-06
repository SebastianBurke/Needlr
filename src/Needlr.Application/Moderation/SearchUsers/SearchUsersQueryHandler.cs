using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Moderation.SearchUsers;

internal sealed class SearchUsersQueryHandler(
    ICurrentUser currentUser,
    IModerationService moderation)
    : IRequestHandler<SearchUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return Result<PagedResult<AdminUserDto>>.Failure(Error.Forbidden("Admin only."));

        var page = await moderation.SearchUsersAsync(
            request.EmailSubstring, request.Role, request.Page, cancellationToken);
        return Result<PagedResult<AdminUserDto>>.Success(page);
    }
}

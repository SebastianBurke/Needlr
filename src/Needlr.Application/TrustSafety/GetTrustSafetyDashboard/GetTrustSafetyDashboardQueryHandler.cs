using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.TrustSafety.GetTrustSafetyDashboard;

internal sealed class GetTrustSafetyDashboardQueryHandler(
    ICurrentUser currentUser,
    ITrustSafetyDashboardService dashboard) : IRequestHandler<GetTrustSafetyDashboardQuery, Result<TrustSafetyDashboardDto>>
{
    public async Task<Result<TrustSafetyDashboardDto>> Handle(
        GetTrustSafetyDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return Result<TrustSafetyDashboardDto>.Failure(Error.Forbidden("Admin only."));

        var dto = await dashboard.GetAsync(cancellationToken);
        return Result<TrustSafetyDashboardDto>.Success(dto);
    }
}

using Hangfire.Dashboard;
using Needlr.Domain.Enums;

namespace Needlr.Api.Hangfire;

/// <summary>
/// Hangfire dashboard auth filter. Allows only authenticated callers in the Admin role.
/// JWT bearer is the project's auth scheme; Hangfire reads the principal from
/// <c>HttpContext.User</c> after the standard middleware runs (we mount the dashboard
/// after <c>UseAuthentication</c> / <c>UseAuthorization</c>).
/// </summary>
internal sealed class AdminOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var user = http.User;
        return user?.Identity?.IsAuthenticated == true && user.IsInRole(nameof(UserRole.Admin));
    }
}

using System.Security.Claims;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;

namespace Needlr.Api.Auth;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the per-request <see cref="HttpContext"/>. Lives in the
/// Api layer (rather than Infrastructure) because <c>IHttpContextAccessor</c> is part of the
/// Microsoft.AspNetCore.App shared framework — pulling it into a class library would require a
/// FrameworkReference, which we'd rather avoid for the Infrastructure project.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role
    {
        get
        {
            var role = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, ignoreCase: false, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(UserRole role) => Role == role;
}

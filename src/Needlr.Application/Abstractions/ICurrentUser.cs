using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions;

/// <summary>
/// Accessor for the current authenticated user. Implemented in Infrastructure on top of
/// <c>IHttpContextAccessor</c>; nullable properties when the request is anonymous.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The current user's id, or null if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The current user's email claim, or null if anonymous / not present.</summary>
    string? Email { get; }

    /// <summary>The current user's role, or null if anonymous / not present.</summary>
    UserRole? Role { get; }

    /// <summary>True if the request has an authenticated principal.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Convenience check; returns false for anonymous requests.</summary>
    bool IsInRole(UserRole role);
}

using Microsoft.AspNetCore.Identity;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Identity;

/// <summary>
/// The authentication record. Lives in Infrastructure (not Domain) because Domain has zero
/// external dependencies except <c>NetTopologySuite.Geometries</c> per ARCHITECTURE.md
/// § Layering rules. Domain entities reference users by <c>UserId : Guid</c> only.
/// </summary>
/// <remarks>
/// Inherits Email, PasswordHash, EmailConfirmed, SecurityStamp, ConcurrencyStamp, etc.
/// from <see cref="IdentityUser{TKey}"/>.
/// </remarks>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>UTC timestamp the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Domain role driving authorization and which profile entity (CustomerProfile / Artist)
    /// the user has a one-to-one relationship with.</summary>
    public UserRole Role { get; set; }
}

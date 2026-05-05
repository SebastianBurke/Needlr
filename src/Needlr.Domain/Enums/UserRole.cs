namespace Needlr.Domain.Enums;

/// <summary>
/// Role assigned to a user account. Drives authorization and which profile entity
/// (CustomerProfile / Artist) the user has a one-to-one relationship with.
/// </summary>
public enum UserRole
{
    Customer,
    Artist,
    Admin
}

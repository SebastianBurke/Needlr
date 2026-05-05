namespace Needlr.Domain.Enums;

/// <summary>
/// Whether a SessionPhoto was uploaded by the artist or by the customer.
/// Denormalized onto SessionPhoto for fast filtering without joining to the user record.
/// </summary>
public enum UploadedByRole
{
    Artist,
    Customer
}

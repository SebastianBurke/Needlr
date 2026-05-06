namespace Needlr.Application.Customers;

/// <summary>The calling customer's editable profile bits + the JWT-claim email (read-only).</summary>
public sealed record MyCustomerProfileDto(
    Guid Id,
    string DisplayName,
    string? Email);

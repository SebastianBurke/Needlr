namespace Needlr.Contracts.Customers;

/// <summary>The calling customer's editable profile + read-only email.</summary>
public sealed record MyCustomerProfileResponse(
    Guid Id,
    string DisplayName,
    string? Email);

/// <summary>Body for PATCH /api/customers/me. v1: display name only.</summary>
public sealed record UpdateMyCustomerProfileRequest(string DisplayName);

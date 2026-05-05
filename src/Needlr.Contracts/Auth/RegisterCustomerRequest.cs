namespace Needlr.Contracts.Auth;

public sealed record RegisterCustomerRequest(string Email, string Password, string DisplayName);

using Needlr.Application.Messaging;

namespace Needlr.Application.Auth.RegisterCustomer;

public sealed record RegisterCustomerCommand(
    string Email,
    string Password,
    string DisplayName) : ICommand<AuthResult>;

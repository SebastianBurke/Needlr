using Needlr.Application.Messaging;

namespace Needlr.Application.Customers.UpdateMyProfile;

/// <summary>Updates the calling customer's editable profile fields. v1: just display name.</summary>
public sealed record UpdateMyProfileCommand(string DisplayName) : ICommand;

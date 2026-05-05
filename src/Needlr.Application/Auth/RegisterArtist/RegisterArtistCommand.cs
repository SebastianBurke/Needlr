using Needlr.Application.Messaging;

namespace Needlr.Application.Auth.RegisterArtist;

/// <summary>
/// Phase-4 minimal artist registration. Per FEATURE_SPECS.md § Artist onboarding, this is
/// step 1 only (account creation); studios, credentials, Stripe Connect, portfolio seed,
/// availability, etc. land in their own commands across Phases 5–11.
/// </summary>
public sealed record RegisterArtistCommand(
    string Email,
    string Password,
    string DisplayName,
    int YearsExperience) : ICommand<AuthResult>;

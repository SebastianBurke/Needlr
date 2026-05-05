namespace Needlr.Contracts.Auth;

public sealed record RegisterArtistRequest(
    string Email,
    string Password,
    string DisplayName,
    int YearsExperience);

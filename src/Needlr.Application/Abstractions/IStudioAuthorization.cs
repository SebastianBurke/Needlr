namespace Needlr.Application.Abstractions;

/// <summary>
/// Authorization helper for studio-scoped commands. Resolves the calling user's underlying
/// Artist row and checks for an Active Founder/Admin affiliation on the target studio.
/// Centralized here so handlers don't repeat the same join.
/// </summary>
public interface IStudioAuthorization
{
    Task<bool> IsCurrentUserStudioAdminAsync(Guid studioId, CancellationToken cancellationToken = default);

    /// <summary>Returns the calling user's Artist id, or null if the caller is not an artist.</summary>
    Task<Guid?> GetCurrentArtistIdAsync(CancellationToken cancellationToken = default);
}

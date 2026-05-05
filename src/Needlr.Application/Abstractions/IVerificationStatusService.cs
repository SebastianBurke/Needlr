using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions;

/// <summary>
/// Computes the discoverable <see cref="VerificationStatus"/> for an artist or studio from
/// their credentials and (for artists) their primary studio's status. Used by the discovery
/// filter, admin dashboard, and the artist profile page.
///
/// Computation rules per FEATURE_SPECS.md § Verification > Discoverability rules:
///   - <b>Studio</b> is <c>Verified</c> when at least one <c>Verified</c> credential exists for
///     every required type per the studio's jurisdiction. <c>DocumentsSubmitted</c> if any
///     credential is uploaded but not all required types are <c>Verified</c>. Otherwise
///     <c>Unverified</c>.
///   - <b>Artist</b> is <c>Verified</c> only if their primary studio is <c>Verified</c> AND they
///     have at least one <c>Verified</c> credential of each artist-level type required by their
///     primary studio's jurisdiction. <c>DocumentsSubmitted</c> if some required pieces are
///     pending. Otherwise <c>Unverified</c>.
///
/// Phase 6 does NOT yet apply expiry-grace transitions (Verified → DocumentsSubmitted →
/// Unverified across the 30-day / 7-day / expiry windows). The nightly job in Phase 14 owns
/// that.
/// </summary>
public interface IVerificationStatusService
{
    Task<VerificationStatus> ComputeStudioStatusAsync(Guid studioId, CancellationToken cancellationToken = default);

    Task<VerificationStatus> ComputeArtistStatusAsync(Guid artistId, CancellationToken cancellationToken = default);
}

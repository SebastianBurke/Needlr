using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Identity;

internal sealed class VerificationStatusService(NeedlrDbContext db) : IVerificationStatusService
{
    private readonly NeedlrDbContext _db = db;

    public async Task<VerificationStatus> ComputeStudioStatusAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        var studio = await _db.Studios.FirstOrDefaultAsync(s => s.Id == studioId, cancellationToken);
        if (studio is null) return VerificationStatus.Unverified;

        var credentials = await _db.StudioCredentials
            .Where(c => c.StudioId == studioId)
            .ToListAsync(cancellationToken);

        // No jurisdiction fan-in here; for Phase 6 we treat the studio as bound to its
        // credentials' jurisdictions. The "are all required types Verified" check uses every
        // jurisdiction in scope (typically a single one — Montréal at launch).
        var jurisdictionIds = credentials.Select(c => c.JurisdictionId).Distinct().ToList();
        if (jurisdictionIds.Count == 0)
            return VerificationStatus.Unverified;

        var jurisdictions = await _db.Jurisdictions
            .Where(j => jurisdictionIds.Contains(j.Id))
            .ToListAsync(cancellationToken);

        var allRequiredVerified = jurisdictions.All(j => StudioJurisdictionFullySatisfied(j, credentials));
        if (allRequiredVerified) return VerificationStatus.Verified;

        return credentials.Any(c => c.VerificationStatus is VerificationStatus.DocumentsSubmitted or VerificationStatus.Verified)
            ? VerificationStatus.DocumentsSubmitted
            : VerificationStatus.Unverified;
    }

    public async Task<VerificationStatus> ComputeArtistStatusAsync(Guid artistId, CancellationToken cancellationToken = default)
    {
        // Resolve primary studio.
        var primary = await _db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId
                && a.IsPrimary
                && a.Status == AffiliationStatus.Active)
            .Select(a => new { a.StudioId })
            .FirstOrDefaultAsync(cancellationToken);
        if (primary is null) return VerificationStatus.Unverified;

        var studioStatus = await ComputeStudioStatusAsync(primary.StudioId, cancellationToken);

        // Artist credentials and the studio's jurisdiction set drive the artist-level required-type check.
        var artistCreds = await _db.ArtistCredentials
            .Where(c => c.ArtistId == artistId)
            .ToListAsync(cancellationToken);

        var jurisdictionIds = await _db.StudioCredentials
            .Where(c => c.StudioId == primary.StudioId)
            .Select(c => c.JurisdictionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (jurisdictionIds.Count == 0)
            return VerificationStatus.Unverified;

        var jurisdictions = await _db.Jurisdictions
            .Where(j => jurisdictionIds.Contains(j.Id))
            .ToListAsync(cancellationToken);

        var allArtistRequirementsVerified = jurisdictions.All(j => ArtistJurisdictionFullySatisfied(j, artistCreds));

        if (studioStatus == VerificationStatus.Verified && allArtistRequirementsVerified)
            return VerificationStatus.Verified;

        if (studioStatus is VerificationStatus.DocumentsSubmitted or VerificationStatus.Verified
            || artistCreds.Any(c => c.VerificationStatus is VerificationStatus.DocumentsSubmitted or VerificationStatus.Verified))
        {
            return VerificationStatus.DocumentsSubmitted;
        }

        return VerificationStatus.Unverified;
    }

    private static bool StudioJurisdictionFullySatisfied(Jurisdiction j, List<StudioCredential> credentials)
    {
        var inJ = credentials.Where(c => c.JurisdictionId == j.Id && c.VerificationStatus == VerificationStatus.Verified).ToList();

        if (j.RequiresStudioInspection && !inJ.Any(c => c.CredentialType == StudioCredentialType.HealthInspection))
            return false;

        return true;
    }

    private static bool ArtistJurisdictionFullySatisfied(Jurisdiction j, List<ArtistCredential> credentials)
    {
        var verified = credentials.Where(c => c.JurisdictionId == j.Id && c.VerificationStatus == VerificationStatus.Verified).ToList();

        if (j.RequiresBloodbornePathogenCert
            && !verified.Any(c => c.CredentialType == ArtistCredentialType.BloodbornePathogenCertification))
            return false;

        if (j.RequiresArtistHygieneTraining
            && !verified.Any(c => c.CredentialType == ArtistCredentialType.FormationHygieneEtSalubrite))
            return false;

        if (j.RequiresArtistLicense
            && !verified.Any(c => c.CredentialType == ArtistCredentialType.LicensePractitioner))
            return false;

        return true;
    }
}

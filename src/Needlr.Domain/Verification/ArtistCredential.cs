using Needlr.Domain.Enums;

namespace Needlr.Domain.Verification;

/// <summary>
/// An artist-level credential (bloodborne pathogen cert, hygiene training, etc.) that contributes
/// to the artist's computed verification status.
/// </summary>
public sealed class ArtistCredential
{
    public const int RejectionReasonMaxLength = 1000;

    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public Guid JurisdictionId { get; init; }
    public ArtistCredentialType CredentialType { get; init; }
    public string? DocumentUrl { get; set; }
    public DateOnly IssuedDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
    public Guid? VerifiedByAdminId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }

    private ArtistCredential() { }

    public ArtistCredential(
        Guid id,
        Guid artistId,
        Guid jurisdictionId,
        ArtistCredentialType credentialType,
        DateOnly issuedDate,
        DateOnly expiryDate,
        string? documentUrl = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (jurisdictionId == Guid.Empty) throw new ArgumentException("JurisdictionId is required.", nameof(jurisdictionId));
        if (expiryDate <= issuedDate)
            throw new ArgumentException("ExpiryDate must be after IssuedDate.", nameof(expiryDate));

        Id = id;
        ArtistId = artistId;
        JurisdictionId = jurisdictionId;
        CredentialType = credentialType;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        DocumentUrl = documentUrl;
    }
}

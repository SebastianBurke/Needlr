using Needlr.Domain.Enums;

namespace Needlr.Domain.Verification;

/// <summary>
/// A studio-level credential (health inspection, municipal registration, etc.) that contributes
/// to the studio's computed verification status.
/// </summary>
public sealed class StudioCredential
{
    public const int RejectionReasonMaxLength = 1000;

    public Guid Id { get; init; }
    public Guid StudioId { get; init; }
    public Guid JurisdictionId { get; init; }
    public StudioCredentialType CredentialType { get; init; }
    public string? DocumentUrl { get; set; }
    public DateOnly IssuedDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;
    public Guid? VerifiedByAdminId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }

    private StudioCredential() { }

    public StudioCredential(
        Guid id,
        Guid studioId,
        Guid jurisdictionId,
        StudioCredentialType credentialType,
        DateOnly issuedDate,
        DateOnly expiryDate,
        string? documentUrl = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (studioId == Guid.Empty) throw new ArgumentException("StudioId is required.", nameof(studioId));
        if (jurisdictionId == Guid.Empty) throw new ArgumentException("JurisdictionId is required.", nameof(jurisdictionId));
        if (expiryDate <= issuedDate)
            throw new ArgumentException("ExpiryDate must be after IssuedDate.", nameof(expiryDate));

        Id = id;
        StudioId = studioId;
        JurisdictionId = jurisdictionId;
        CredentialType = credentialType;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        DocumentUrl = documentUrl;
    }
}

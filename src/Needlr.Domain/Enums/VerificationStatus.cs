namespace Needlr.Domain.Enums;

/// <summary>
/// Verification state for a credential (Studio or Artist). Used to compute artist/studio
/// discoverability per FEATURE_SPECS.md § Discoverability rules.
/// </summary>
public enum VerificationStatus
{
    /// <summary>No documents submitted, or documents present but none verified.</summary>
    Unverified,

    /// <summary>At least one document uploaded; awaiting admin review.</summary>
    DocumentsSubmitted,

    /// <summary>Document reviewed and approved by an admin.</summary>
    Verified,

    /// <summary>Document reviewed and rejected (with a reason recorded).</summary>
    Rejected,

    /// <summary>Document was Verified but its expiry date has passed (post-grace-period).</summary>
    Expired
}

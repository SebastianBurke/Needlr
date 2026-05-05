namespace Needlr.Domain.Enums;

/// <summary>
/// Lifecycle status of an ArtistStudioAffiliation.
/// </summary>
public enum AffiliationStatus
{
    /// <summary>Request or invitation has been issued but not yet accepted by the other party.</summary>
    Pending,

    /// <summary>Affiliation is in effect; the artist appears on the studio's roster.</summary>
    Active,

    /// <summary>Affiliation has ended (left the studio, removed by admin, guest spot end date reached).</summary>
    Ended,

    /// <summary>Request or invitation was rejected by the other party.</summary>
    Rejected
}

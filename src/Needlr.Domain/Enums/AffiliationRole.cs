namespace Needlr.Domain.Enums;

/// <summary>
/// Permission level held by an artist on an ArtistStudioAffiliation. Per ADR-004 there is no
/// separate StudioOwner role — studio admin rights are a per-affiliation permission held by an artist.
/// </summary>
public enum AffiliationRole
{
    /// <summary>The artist who originally created the studio. Always implies Admin permissions.</summary>
    Founder,

    /// <summary>Can edit studio info, manage join policy, invite/approve/remove artists, promote members.</summary>
    Admin,

    /// <summary>Standard affiliated artist; appears on the studio roster but has no admin powers.</summary>
    Member
}

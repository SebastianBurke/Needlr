namespace Needlr.Domain.Enums;

/// <summary>
/// Controls how new artists can become affiliated with a studio.
/// </summary>
public enum JoinPolicy
{
    /// <summary>Verified artists may submit a join request; a studio admin approves or rejects.</summary>
    Open,

    /// <summary>Only studio admins can initiate via invitations to specific artists.</summary>
    InviteOnly,

    /// <summary>No new members. Existing members only; no requests, no invites.</summary>
    Closed
}

namespace Needlr.Domain.Enums;

/// <summary>
/// Lifecycle state of a MessageThread. Transitions Active -> Locked at terminal+90 days
/// per ADR-003 and FEATURE_SPECS.md § Booking lifecycle post-confirmation.
/// </summary>
public enum MessageThreadStatus
{
    /// <summary>Both parties can send messages.</summary>
    Active,

    /// <summary>Existing messages remain visible; no new messages may be sent.</summary>
    Locked
}

namespace Needlr.Domain.Enums;

/// <summary>
/// Studio category. Largely informational at launch; may drive UI variations later
/// (e.g., Solo studios show artist name as the headline instead of studio name).
/// </summary>
public enum StudioType
{
    /// <summary>Traditional brick-and-mortar with multiple artists.</summary>
    Shop,

    /// <summary>Single-artist working location (artist's own private studio with just them).</summary>
    Solo,

    /// <summary>Invite-only studio that is not publicly walk-in-able.</summary>
    Private
}

namespace Needlr.Domain.Enums;

/// <summary>
/// Whether a SessionPhoto is the fresh post-session image (uploaded by artist) or the
/// healed image (uploaded by customer at the 4-month mark).
/// </summary>
public enum PhotoType
{
    Fresh,
    Healed
}

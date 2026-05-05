namespace Needlr.Domain.Enums;

/// <summary>
/// Outcome of an admin's review of a MessageReport.
/// </summary>
public enum MessageReportResolution
{
    NoAction,
    MessageHidden,
    UserWarned,
    UserSuspended
}

namespace Needlr.Domain.Enums;

/// <summary>
/// Why a message was reported.
/// </summary>
public enum MessageReportReason
{
    Harassment,
    OffensiveContent,
    Spam,

    /// <summary>The message attempts to move the conversation off-platform pre-confirmation.</summary>
    OffPlatformSolicitation,

    Other
}

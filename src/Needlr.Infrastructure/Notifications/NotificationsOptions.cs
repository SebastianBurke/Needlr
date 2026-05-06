namespace Needlr.Infrastructure.Notifications;

/// <summary>
/// Bound from the <c>Notifications</c> config section. Fully optional — when absent, the
/// dispatcher uses the console-logging fallbacks. Setting <see cref="SendGridApiKey"/>
/// switches the email channel to SendGrid; setting <see cref="VapidPublicKey"/> +
/// <see cref="VapidPrivateKey"/> + <see cref="VapidSubject"/> activates Web Push.
/// </summary>
public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    /// <summary>From-address used by the email sender. Domain must be verified in SendGrid.</summary>
    public string FromEmail { get; init; } = "noreply@needlr.app";

    /// <summary>Display name used by the email sender's "From" header.</summary>
    public string FromName { get; init; } = "Needlr";

    /// <summary>SendGrid API key (<c>SG.…</c>). When null/empty, the email sender logs to the console.</summary>
    public string? SendGridApiKey { get; init; }

    /// <summary>VAPID public key (base64url) for Web Push. Required to actually send pushes.</summary>
    public string? VapidPublicKey { get; init; }

    /// <summary>VAPID private key (base64url). Pairs with <see cref="VapidPublicKey"/>.</summary>
    public string? VapidPrivateKey { get; init; }

    /// <summary>VAPID "subject" (mailto:foo@bar). Required by the Web Push spec.</summary>
    public string? VapidSubject { get; init; }
}

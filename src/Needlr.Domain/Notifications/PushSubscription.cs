namespace Needlr.Domain.Notifications;

/// <summary>
/// One Web Push subscription per browser per user. The endpoint URL is the natural
/// identifier — re-registering the same browser updates the keys in place rather than
/// inserting a duplicate. Keys are public-side (P256dh + Auth from the browser's
/// PushSubscription API); the platform VAPID private key lives in <c>NotificationsOptions</c>.
/// </summary>
public sealed class PushSubscription
{
    public const int EndpointMaxLength = 1000;
    public const int P256dhMaxLength = 200;
    public const int AuthMaxLength = 100;

    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSentAt { get; set; }

    private PushSubscription() { }

    public PushSubscription(
        Guid id, Guid userId, string endpoint, string p256dh, string auth, DateTime createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(p256dh);
        ArgumentException.ThrowIfNullOrWhiteSpace(auth);
        if (endpoint.Length > EndpointMaxLength)
            throw new ArgumentException($"Endpoint must be <= {EndpointMaxLength} chars.", nameof(endpoint));
        if (p256dh.Length > P256dhMaxLength)
            throw new ArgumentException($"P256dh must be <= {P256dhMaxLength} chars.", nameof(p256dh));
        if (auth.Length > AuthMaxLength)
            throw new ArgumentException($"Auth must be <= {AuthMaxLength} chars.", nameof(auth));
        if (createdAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("CreatedAt must be UTC.", nameof(createdAt));

        Id = id;
        UserId = userId;
        Endpoint = endpoint;
        P256dh = p256dh;
        Auth = auth;
        CreatedAt = createdAt;
    }
}

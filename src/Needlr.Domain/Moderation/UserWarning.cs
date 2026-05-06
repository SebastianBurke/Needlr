namespace Needlr.Domain.Moderation;

/// <summary>
/// Audit row for an admin-issued warning to a user (FEATURE_SPECS.md § Admin actions).
/// Phase 15 stores the warning + reason; the user-facing presentation is FE work in
/// Phase 22.
/// </summary>
public sealed class UserWarning
{
    public const int ReasonMaxLength = 2000;

    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid IssuedByAdminId { get; init; }
    public string Reason { get; init; } = null!;
    public DateTime IssuedAt { get; init; }

    private UserWarning() { }

    public UserWarning(Guid id, Guid userId, Guid issuedByAdminId, string reason, DateTime issuedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (issuedByAdminId == Guid.Empty)
            throw new ArgumentException("IssuedByAdminId is required.", nameof(issuedByAdminId));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Length > ReasonMaxLength)
            throw new ArgumentException($"Reason must be <= {ReasonMaxLength} chars.", nameof(reason));
        if (issuedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("IssuedAt must be UTC.", nameof(issuedAt));

        Id = id;
        UserId = userId;
        IssuedByAdminId = issuedByAdminId;
        Reason = reason.Trim();
        IssuedAt = issuedAt;
    }
}

namespace Needlr.Application.Abstractions;

/// <summary>
/// Admin moderation actions on the user record. Lives behind an abstraction because
/// <c>ApplicationUser</c> is in Infrastructure (Identity) — Application can't talk to it
/// directly. The impl flips suspension fields and reads is-suspended status. UserWarning
/// audit rows are written by the warn-user handler via the standard repository pattern.
/// </summary>
public interface IModerationService
{
    /// <summary>True if the user has a non-null <c>SuspendedAt</c>.</summary>
    Task<bool> IsSuspendedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sets <c>SuspendedAt = clock.UtcNow</c> + records the reason. Idempotent.</summary>
    Task<bool> SuspendAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Clears suspension fields. Idempotent.</summary>
    Task<bool> UnsuspendAsync(Guid userId, CancellationToken cancellationToken = default);
}

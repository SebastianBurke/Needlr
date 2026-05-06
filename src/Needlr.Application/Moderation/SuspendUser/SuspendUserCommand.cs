using Needlr.Application.Messaging;

namespace Needlr.Application.Moderation.SuspendUser;

/// <summary>
/// Admin suspends a user (artist or customer). Per FEATURE_SPECS.md § Admin actions:
/// suspended artists are invisible in discovery and can't accept new bookings; suspended
/// customers can't make new bookings. Existing bookings are honored regardless.
/// Idempotent — re-suspending an already-suspended user is a no-op (won't reset the
/// SuspendedAt stamp).
/// </summary>
public sealed record SuspendUserCommand(Guid UserId, string Reason) : ICommand;

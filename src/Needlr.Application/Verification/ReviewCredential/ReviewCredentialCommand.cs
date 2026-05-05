using Needlr.Application.Messaging;

namespace Needlr.Application.Verification.ReviewCredential;

/// <summary>
/// Admin approves or rejects a credential. <see cref="RejectionReason"/> is required when
/// <see cref="Approve"/> is false. The <see cref="Kind"/> discriminator picks between studio
/// and artist credential tables.
/// </summary>
public sealed record ReviewCredentialCommand(
    CredentialKind Kind,
    Guid CredentialId,
    bool Approve,
    string? RejectionReason = null) : ICommand;

namespace Needlr.Application.Verification;

public sealed record VerificationQueueItemDto(
    Guid Id,
    CredentialKind Kind,
    Guid OwnerEntityId,
    string CredentialType,
    string? DocumentUrl,
    DateOnly IssuedDate,
    DateOnly ExpiryDate);

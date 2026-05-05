namespace Needlr.Contracts.Verification;

// ---- Requests ----

/// <summary>Form fields for credential upload (file is sent as a multipart "file" part).</summary>
public sealed record UploadCredentialRequest(
    Guid JurisdictionId,
    string CredentialType,
    DateOnly IssuedDate,
    DateOnly ExpiryDate);

public sealed record ReviewCredentialRequest(bool Approve, string? RejectionReason = null);

// ---- Responses ----

public sealed record VerificationQueueItemResponse(
    Guid Id,
    string Kind,                // "Studio" or "Artist"
    Guid OwnerEntityId,
    string CredentialType,
    string? DocumentUrl,
    DateOnly IssuedDate,
    DateOnly ExpiryDate);

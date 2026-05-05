using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Verification.UploadStudioCredential;

/// <summary>
/// Studio admin uploads a studio-level credential document (RSSS health inspection,
/// municipal registration, etc.). Persists the uploaded blob via <see cref="Application.Abstractions.IImageStorage"/>
/// and writes a <see cref="Domain.Verification.StudioCredential"/> with <see cref="VerificationStatus.DocumentsSubmitted"/>.
/// The stream is consumed once and not retained on the command.
/// </summary>
public sealed record UploadStudioCredentialCommand(
    Guid StudioId,
    Guid JurisdictionId,
    StudioCredentialType CredentialType,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    Stream FileContent,
    string ContentType,
    string OriginalFilename) : ICommand<Guid>;

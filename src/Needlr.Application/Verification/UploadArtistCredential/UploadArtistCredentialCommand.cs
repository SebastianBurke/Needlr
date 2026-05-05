using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Verification.UploadArtistCredential;

public sealed record UploadArtistCredentialCommand(
    Guid JurisdictionId,
    ArtistCredentialType CredentialType,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    Stream FileContent,
    string ContentType,
    string OriginalFilename) : ICommand<Guid>;

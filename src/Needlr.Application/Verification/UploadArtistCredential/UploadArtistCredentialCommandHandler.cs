using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Verification;

namespace Needlr.Application.Verification.UploadArtistCredential;

internal sealed class UploadArtistCredentialCommandHandler(
    IStudioAuthorization studioAuthorization,
    IImageStorage imageStorage,
    IArtistCredentialRepository credentials)
    : IRequestHandler<UploadArtistCredentialCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadArtistCredentialCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can upload artist credentials."));

        var key = await imageStorage.UploadAsync(
            request.FileContent,
            request.ContentType,
            keyPrefix: $"artist-credentials/{artistId.Value:N}",
            cancellationToken);

        var credential = new ArtistCredential(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            jurisdictionId: request.JurisdictionId,
            credentialType: request.CredentialType,
            issuedDate: request.IssuedDate,
            expiryDate: request.ExpiryDate,
            documentUrl: key);
        credential.VerificationStatus = Domain.Enums.VerificationStatus.DocumentsSubmitted;
        credentials.Add(credential);

        return Result<Guid>.Success(credential.Id);
    }
}

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Verification;

namespace Needlr.Application.Verification.UploadStudioCredential;

internal sealed class UploadStudioCredentialCommandHandler(
    IStudioAuthorization studioAuthorization,
    IImageStorage imageStorage,
    IStudioCredentialRepository credentials)
    : IRequestHandler<UploadStudioCredentialCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadStudioCredentialCommand request, CancellationToken cancellationToken)
    {
        if (!await studioAuthorization.IsCurrentUserStudioAdminAsync(request.StudioId, cancellationToken))
            return Result<Guid>.Failure(Error.Forbidden("Only studio admins can upload studio credentials."));

        var key = await imageStorage.UploadAsync(
            request.FileContent,
            request.ContentType,
            keyPrefix: $"studio-credentials/{request.StudioId:N}",
            cancellationToken);

        var credential = new StudioCredential(
            id: Guid.NewGuid(),
            studioId: request.StudioId,
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

using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Verification.ReviewCredential;

internal sealed class ReviewCredentialCommandHandler(
    ICurrentUser currentUser,
    IStudioCredentialRepository studioCredentials,
    IArtistCredentialRepository artistCredentials,
    IClock clock) : IRequestHandler<ReviewCredentialCommand, Result>
{
    public async Task<Result> Handle(ReviewCredentialCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            return Result.Failure(Error.Forbidden("Only admins can review credentials."));

        var adminId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated admin must have a UserId claim.");

        var newStatus = request.Approve ? VerificationStatus.Verified : VerificationStatus.Rejected;
        var verifiedAt = clock.UtcNow;

        if (request.Kind == CredentialKind.Studio)
        {
            var cred = await studioCredentials.GetByIdAsync(request.CredentialId, cancellationToken);
            if (cred is null) return Result.Failure(Error.NotFound("StudioCredential"));
            if (cred.VerificationStatus != VerificationStatus.DocumentsSubmitted)
                return Result.Failure(Error.FailedPrecondition(
                    "Only credentials in DocumentsSubmitted status can be reviewed."));

            cred.VerificationStatus = newStatus;
            cred.VerifiedByAdminId = adminId;
            cred.VerifiedAt = verifiedAt;
            cred.RejectionReason = request.Approve ? null : request.RejectionReason;
        }
        else
        {
            var cred = await artistCredentials.GetByIdAsync(request.CredentialId, cancellationToken);
            if (cred is null) return Result.Failure(Error.NotFound("ArtistCredential"));
            if (cred.VerificationStatus != VerificationStatus.DocumentsSubmitted)
                return Result.Failure(Error.FailedPrecondition(
                    "Only credentials in DocumentsSubmitted status can be reviewed."));

            cred.VerificationStatus = newStatus;
            cred.VerifiedByAdminId = adminId;
            cred.VerifiedAt = verifiedAt;
            cred.RejectionReason = request.Approve ? null : request.RejectionReason;
        }

        return Result.Success();
    }
}

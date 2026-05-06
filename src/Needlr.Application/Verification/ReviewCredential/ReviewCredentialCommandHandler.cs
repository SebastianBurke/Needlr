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
    IArtistRepository artists,
    IArtistStudioAffiliationRepository affiliations,
    INotificationDispatcher notifications,
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

        // Set of recipient UserIds we should notify about the result.
        IReadOnlyList<Guid> recipientUserIds;

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

            // Studio credentials notify every Active admin on the studio's roster.
            var roster = await affiliations.ListByStudioAsync(
                cred.StudioId, AffiliationStatus.Active, cancellationToken);
            var userIds = new List<Guid>();
            foreach (var aff in roster.Where(a => a.Role is AffiliationRole.Founder or AffiliationRole.Admin))
            {
                var artist = await artists.GetByIdAsync(aff.ArtistId, cancellationToken);
                if (artist is not null) userIds.Add(artist.UserId);
            }
            recipientUserIds = userIds;
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

            var artist = await artists.GetByIdAsync(cred.ArtistId, cancellationToken);
            recipientUserIds = artist is null ? Array.Empty<Guid>() : new[] { artist.UserId };
        }

        var notificationType = request.Approve
            ? NotificationType.VerificationApproved
            : NotificationType.VerificationRejected;
        var content = request.Approve
            ? new NotificationContent(
                "Verification approved",
                "Your credential has been verified. You're now visible in discovery.",
                "Verified",
                "Your credential is now verified")
            : new NotificationContent(
                "Verification rejected",
                $"Your credential was rejected. Reason: {request.RejectionReason ?? "(none provided)"}.",
                "Verification rejected",
                request.RejectionReason ?? "Open Needlr for details");
        foreach (var uid in recipientUserIds)
            await notifications.DispatchAsync(uid, notificationType, content, cancellationToken);

        return Result.Success();
    }
}

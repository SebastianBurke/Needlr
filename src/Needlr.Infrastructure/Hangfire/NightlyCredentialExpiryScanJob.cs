using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using DomainArtistCredential = Needlr.Domain.Verification.ArtistCredential;
using DomainStudioCredential = Needlr.Domain.Verification.StudioCredential;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Daily sweep that warns at 30d/7d before expiry and downgrades Verified → Expired on
/// or after the expiry date. Idempotent at day-granularity: warnings fire on exact-date
/// matches (today + 30, today + 7) and the Expired downgrade only triggers when status is
/// still Verified, so a re-run on the same day either re-fires the same warning (rare;
/// Hangfire's server lock makes once-per-day the norm) or no-ops on an already-Expired row.
/// </summary>
public sealed class NightlyCredentialExpiryScanJob(
    NeedlrDbContext db,
    IArtistRepository artists,
    IArtistStudioAffiliationRepository affiliations,
    INotificationDispatcher notifications,
    IClock clock,
    ILogger<NightlyCredentialExpiryScanJob> logger)
{
    public const string JobId = "nightly-credential-expiry-scan";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow);
        var in30Days = today.AddDays(30);
        var in7Days = today.AddDays(7);

        var artistCreds = await db.ArtistCredentials
            .Where(c => c.VerificationStatus == VerificationStatus.Verified)
            .Where(c => c.ExpiryDate <= in30Days)
            .ToListAsync(cancellationToken);

        var studioCreds = await db.StudioCredentials
            .Where(c => c.VerificationStatus == VerificationStatus.Verified)
            .Where(c => c.ExpiryDate <= in30Days)
            .ToListAsync(cancellationToken);

        var (artistWarn30, artistWarn7, artistExpired) = (0, 0, 0);
        foreach (var cred in artistCreds)
        {
            try
            {
                var recipient = await ResolveArtistOwnerUserIdAsync(cred.ArtistId, cancellationToken);
                if (recipient is null) continue;

                if (cred.ExpiryDate <= today)
                {
                    cred.VerificationStatus = VerificationStatus.Expired;
                    await notifications.DispatchAsync(recipient.Value, NotificationType.CredentialExpired,
                        Content("Credential expired",
                            "Your credential has expired. Re-upload to restore your verified status."),
                        cancellationToken);
                    artistExpired++;
                }
                else if (cred.ExpiryDate == in7Days)
                {
                    await notifications.DispatchAsync(recipient.Value, NotificationType.CredentialExpiring7d,
                        Content("Credential expiring in 7 days",
                            "One of your credentials expires in a week. Re-upload to avoid a verification gap."),
                        cancellationToken);
                    artistWarn7++;
                }
                else if (cred.ExpiryDate == in30Days)
                {
                    await notifications.DispatchAsync(recipient.Value, NotificationType.CredentialExpiring30d,
                        Content("Credential expiring in 30 days",
                            "One of your credentials expires in 30 days."),
                        cancellationToken);
                    artistWarn30++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Artist credential expiry scan failed for {CredId}.", cred.Id);
            }
        }

        var (studioWarn30, studioWarn7, studioExpired) = (0, 0, 0);
        foreach (var cred in studioCreds)
        {
            try
            {
                var recipients = await ResolveStudioAdminUserIdsAsync(cred.StudioId, cancellationToken);
                if (recipients.Count == 0) continue;

                if (cred.ExpiryDate <= today)
                {
                    cred.VerificationStatus = VerificationStatus.Expired;
                    await NotifyAllAsync(recipients, NotificationType.CredentialExpired,
                        Content("Studio credential expired",
                            "A studio credential has expired. The studio is no longer Verified until re-uploaded."),
                        cancellationToken);
                    studioExpired++;
                }
                else if (cred.ExpiryDate == in7Days)
                {
                    await NotifyAllAsync(recipients, NotificationType.CredentialExpiring7d,
                        Content("Studio credential expiring in 7 days",
                            "A studio credential expires in a week."),
                        cancellationToken);
                    studioWarn7++;
                }
                else if (cred.ExpiryDate == in30Days)
                {
                    await NotifyAllAsync(recipients, NotificationType.CredentialExpiring30d,
                        Content("Studio credential expiring in 30 days",
                            "A studio credential expires in 30 days."),
                        cancellationToken);
                    studioWarn30++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Studio credential expiry scan failed for {CredId}.", cred.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Credential expiry scan: artist 30d={A30} 7d={A7} expired={AExp}; studio 30d={S30} 7d={S7} expired={SExp}.",
            artistWarn30, artistWarn7, artistExpired, studioWarn30, studioWarn7, studioExpired);
    }

    private async Task<Guid?> ResolveArtistOwnerUserIdAsync(Guid artistId, CancellationToken cancellationToken)
    {
        var artist = await artists.GetByIdAsync(artistId, cancellationToken);
        return artist?.UserId;
    }

    private async Task<IReadOnlyList<Guid>> ResolveStudioAdminUserIdsAsync(Guid studioId, CancellationToken cancellationToken)
    {
        var roster = await affiliations.ListByStudioAsync(studioId, AffiliationStatus.Active, cancellationToken);
        var userIds = new List<Guid>();
        foreach (var aff in roster.Where(a => a.Role is AffiliationRole.Founder or AffiliationRole.Admin))
        {
            var artist = await artists.GetByIdAsync(aff.ArtistId, cancellationToken);
            if (artist is not null) userIds.Add(artist.UserId);
        }
        return userIds;
    }

    private async Task NotifyAllAsync(
        IReadOnlyList<Guid> userIds, NotificationType type, NotificationContent content, CancellationToken cancellationToken)
    {
        foreach (var uid in userIds)
            await notifications.DispatchAsync(uid, type, content, cancellationToken);
    }

    private static NotificationContent Content(string title, string body) =>
        new(EmailSubject: title, EmailBody: body, PushTitle: title, PushBody: body);
}

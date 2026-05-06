using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Purges attachment blobs for bookings whose terminal-state anchor (Completed / Cancelled /
/// Declined / Expired) is &gt;= 1 year ago. Deletes the underlying object-storage file via
/// <see cref="IImageStorage"/>, clears <c>BookingAttachment.Url</c>, and sets
/// <c>Booking.IsAttachmentsPurged</c>. Per ADR-003 § Retention, message *bodies* are not
/// touched — only attachment blobs.
/// </summary>
public sealed class NightlyBookingAttachmentPurgeJob(
    NeedlrDbContext db,
    IImageStorage imageStorage,
    IClock clock,
    ILogger<NightlyBookingAttachmentPurgeJob> logger)
{
    public const string JobId = "nightly-booking-attachment-purge";
    public const int RetainDays = 365;

    private static readonly BookingStatus[] TerminalStatuses =
    [
        BookingStatus.Completed,
        BookingStatus.CancelledByArtist,
        BookingStatus.CancelledByCustomer,
        BookingStatus.Declined,
        BookingStatus.Expired
    ];

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = clock.UtcNow.AddDays(-RetainDays);

        // "Anchor" timestamp for terminality: prefer CompletedAt; otherwise fall back to
        // RequestedAt (cancelled-pre-accept records have no CompletedAt). Pre-fetch the
        // candidates by id so the per-booking purge runs in its own change-tracker scope.
        var dueIds = await db.Bookings
            .Where(b => !b.IsAttachmentsPurged
                && TerminalStatuses.Contains(b.Status)
                && (b.CompletedAt ?? b.RequestedAt) <= cutoff)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var purged = 0;
        var failures = 0;

        foreach (var bookingId in dueIds)
        {
            try
            {
                await PurgeOneAsync(bookingId, cancellationToken);
                purged++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Attachment purge failed for booking {BookingId}.", bookingId);
            }
        }

        logger.LogInformation(
            "Purged attachments for {Purged}/{Total} booking(s); {Failures} failure(s).",
            purged, dueIds.Count, failures);
    }

    private async Task PurgeOneAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        // Booking attachments: BookingId set.
        var bookingAttachments = await db.BookingAttachments
            .Where(a => a.BookingId == bookingId && a.Url != null)
            .ToListAsync(cancellationToken);

        // Message attachments on the booking's thread: MessageId is set, walk Message → Thread → Booking.
        var messageAttachments = await db.BookingAttachments
            .Where(a => a.MessageId != null && a.Url != null
                && db.Messages.Any(m => m.Id == a.MessageId
                    && db.MessageThreads.Any(t => t.Id == m.ThreadId && t.BookingId == bookingId)))
            .ToListAsync(cancellationToken);

        foreach (var att in bookingAttachments.Concat(messageAttachments))
        {
            try
            {
                await imageStorage.DeleteAsync(att.Url!, cancellationToken);
            }
            catch (Exception ex)
            {
                // Already-missing blob is fine — a previous run may have deleted it before
                // the DB row was updated. Log and continue clearing the row.
                logger.LogWarning(ex, "Failed to delete blob {Key}; clearing DB row anyway.", att.Url);
            }
            att.Url = null;
        }

        var booking = await db.Bookings.FirstAsync(b => b.Id == bookingId, cancellationToken);
        booking.IsAttachmentsPurged = true;

        await db.SaveChangesAsync(cancellationToken);
    }
}

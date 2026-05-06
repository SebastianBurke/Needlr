using MediatR;
using Microsoft.Extensions.Logging;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Bookings.ExpireRequestedBooking;

namespace Needlr.Infrastructure.Hangfire;

/// <summary>
/// Hangfire-friendly recurring job that expires Requested bookings older than the 7-day
/// auto-decline cutoff (FEATURE_SPECS.md § Artist response options). Phase 10 ships the job
/// class; Phase 14 wires <c>RecurringJob.AddOrUpdate</c> + <c>AddHangfireServer</c>.
/// </summary>
public sealed class ExpireDueRequestedBookingsRecurringJob(
    IBookingRepository bookings,
    IMediator mediator,
    IClock clock,
    ILogger<ExpireDueRequestedBookingsRecurringJob> logger)
{
    public const string JobId = "expire-due-requested-bookings";
    public const int ExpiryAfterDays = 7;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = clock.UtcNow.AddDays(-ExpiryAfterDays);
        var due = await bookings.ListRequestedExpiredAsync(cutoff, cancellationToken);

        var failures = 0;
        foreach (var booking in due)
        {
            try
            {
                var result = await mediator.Send(new ExpireRequestedBookingCommand(booking.Id), cancellationToken);
                if (result.IsFailure)
                {
                    failures++;
                    logger.LogWarning("Failed to expire booking {BookingId}: {Error}",
                        booking.Id, result.FirstError?.Message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures++;
                logger.LogError(ex, "Failed to expire booking {BookingId}", booking.Id);
            }
        }

        logger.LogInformation(
            "Expired {Total} stale booking(s); {Failures} failure(s).",
            due.Count - failures, failures);
    }
}

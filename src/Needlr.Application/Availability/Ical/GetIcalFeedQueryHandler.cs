using System.Globalization;
using System.Text;
using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Bookings;

namespace Needlr.Application.Availability.Ical;

internal sealed class GetIcalFeedQueryHandler(
    IArtistRepository artists,
    IBookingRepository bookings,
    IClock clock) : IRequestHandler<GetIcalFeedQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetIcalFeedQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result<string>.Failure(Error.NotFound("Feed"));

        var artist = await artists.GetByIdAsync(request.ArtistId, cancellationToken);
        if (artist is null || artist.IcalToken is null
            || !CryptographicEquals(artist.IcalToken, request.Token))
        {
            // NotFound (not Unauthorized) so token-guessing probes can't distinguish "artist
            // exists" from "wrong token". Calendar clients treat 404 as "feed not available".
            return Result<string>.Failure(Error.NotFound("Feed"));
        }

        // Confirmed bookings only, looking 30 days back so very-recent past bookings still
        // appear on subscribers' calendars. Rolling forward is unbounded — calendar clients
        // expect indefinite feeds.
        var fromUtc = clock.UtcNow.AddDays(-30);
        var rows = await bookings.ListConfirmedForArtistFromAsync(artist.Id, fromUtc, cancellationToken);

        var ics = Render(artist.DisplayName, rows);
        return Result<string>.Success(ics);
    }

    private static string Render(string artistName, IReadOnlyList<Booking> bookings)
    {
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//Needlr//Artist Bookings//EN\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:PUBLISH\r\n");
        sb.Append("X-WR-CALNAME:").Append(EscapeText(artistName)).Append(" — Needlr\r\n");

        foreach (var b in bookings)
        {
            // Defensive: only render rows we have a confirmed timestamp for.
            if (b.ConfirmedSessionDate is not { } start) continue;
            var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
            var hours = (double)b.EstimatedDurationHours;
            var endUtc = startUtc.AddHours(hours);

            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append("UID:").Append(b.Id.ToString("D")).Append("@needlr.app\r\n");
            sb.Append("DTSTAMP:").Append(FormatUtc(DateTime.UtcNow)).Append("\r\n");
            sb.Append("DTSTART:").Append(FormatUtc(startUtc)).Append("\r\n");
            sb.Append("DTEND:").Append(FormatUtc(endUtc)).Append("\r\n");
            sb.Append("SUMMARY:").Append(EscapeText($"Booking · {b.BookingType}")).Append("\r\n");
            sb.Append("STATUS:").Append(StatusFor(b)).Append("\r\n");
            sb.Append("END:VEVENT\r\n");
        }

        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    private static string FormatUtc(DateTime dt) =>
        dt.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string StatusFor(Booking b) => b.Status switch
    {
        Domain.Enums.BookingStatus.Completed => "CONFIRMED",
        Domain.Enums.BookingStatus.InProgress => "CONFIRMED",
        Domain.Enums.BookingStatus.Confirmed => "CONFIRMED",
        Domain.Enums.BookingStatus.DepositCaptured => "CONFIRMED",
        Domain.Enums.BookingStatus.Accepted => "TENTATIVE",
        _ => "TENTATIVE"
    };

    private static string EscapeText(string raw) =>
        raw.Replace("\\", "\\\\")
            .Replace(",", "\\,")
            .Replace(";", "\\;")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");

    private static bool CryptographicEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Hangfire;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Hangfire;

/// <summary>
/// Phase 14 recurring job classes are exercised by invoking <c>RunAsync</c> directly.
/// We don't spin up the Hangfire server in tests — those tests are about the per-job
/// logic, not about Hangfire's scheduler.
/// </summary>
public class RecurringJobTests : IClassFixture<WebAppFixture>
{
    private static readonly Guid MontrealJurisdictionId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly WebAppFixture _fixture;

    public RecurringJobTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    // ---- NightlyBookingAttachmentPurgeJob ----

    [Fact]
    public async Task AttachmentPurge_ClearsUrl_AndStampsBooking_ForOldTerminalBookings()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await SeedTerminalBookingAsync(
            artistId, customerAuth.UserId, BookingStatus.Completed, completedDaysAgo: 400);

        await SeedBookingAttachmentAsync(bookingId);

        // Run the job.
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<NightlyBookingAttachmentPurgeJob>();
        await job.RunAsync();

        await using var verify = _fixture.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        booking.IsAttachmentsPurged.Should().BeTrue();
        var atts = await db.BookingAttachments.AsNoTracking()
            .Where(a => a.BookingId == bookingId).ToListAsync();
        atts.Should().NotBeEmpty();
        atts.Should().AllSatisfy(a => a.Url.Should().BeNull());
    }

    [Fact]
    public async Task AttachmentPurge_LeavesRecentTerminalBookingsAlone()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        // 30 days ago — well under the 365 retention.
        var bookingId = await SeedTerminalBookingAsync(
            artistId, customerAuth.UserId, BookingStatus.Completed, completedDaysAgo: 30);
        await SeedBookingAttachmentAsync(bookingId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<NightlyBookingAttachmentPurgeJob>();
        await job.RunAsync();

        await using var verify = _fixture.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        booking.IsAttachmentsPurged.Should().BeFalse();
        var atts = await db.BookingAttachments.AsNoTracking()
            .Where(a => a.BookingId == bookingId).ToListAsync();
        atts.Should().AllSatisfy(a => a.Url.Should().NotBeNullOrEmpty());
    }

    // ---- NightlyCredentialExpiryScanJob ----

    [Fact]
    public async Task CredentialExpiry_DowngradesPastDueArtistCred_AndNotifiesOwner()
    {
        await ResetEmailsAsync();
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        var credId = await SeedArtistCredentialAsync(
            artistId,
            issued: DateTime.UtcNow.AddYears(-1).Date,
            expiry: DateTime.UtcNow.AddDays(-1).Date,
            status: VerificationStatus.Verified);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<NightlyCredentialExpiryScanJob>();
        await job.RunAsync();

        await using var verify = _fixture.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var cred = await db.ArtistCredentials.AsNoTracking().FirstAsync(c => c.Id == credId);
        cred.VerificationStatus.Should().Be(VerificationStatus.Expired);

        var artistEmail = await GetUserEmailAsync(artistAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s =>
            s.To == artistEmail && s.Subject.Contains("expired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CredentialExpiry_30dWarning_FiresOnExactDate()
    {
        await ResetEmailsAsync();
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        var todayUtc = DateTime.UtcNow.Date;
        await SeedArtistCredentialAsync(
            artistId,
            issued: todayUtc.AddYears(-1),
            expiry: todayUtc.AddDays(30),
            status: VerificationStatus.Verified);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<NightlyCredentialExpiryScanJob>();
        await job.RunAsync();

        var artistEmail = await GetUserEmailAsync(artistAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s =>
            s.To == artistEmail && s.Subject.Contains("30 days", StringComparison.OrdinalIgnoreCase));
    }

    // ---- DailyHealedPhotoPromptJob ----

    [Fact]
    public async Task HealedPhotoPrompt_FiresOnceForCompleted4MonthsAgo()
    {
        await ResetEmailsAsync();
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await SeedTerminalBookingAsync(
            artistId, customerAuth.UserId, BookingStatus.Completed, completedDaysAgo: 130);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<DailyHealedPhotoPromptJob>();

        await job.RunAsync();
        await job.RunAsync(); // re-run; should be idempotent

        var customerEmail = await GetUserEmailAsync(customerAuth.UserId);
        _fixture.Emails.Sent.Count(s =>
            s.To == customerEmail && s.Subject.Contains("healed", StringComparison.OrdinalIgnoreCase))
            .Should().Be(1);

        await using var verify = _fixture.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        booking.HealedPhotoPromptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HealedPhotoPrompt_DoesNothingForRecentBookings()
    {
        await ResetEmailsAsync();
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        await SeedTerminalBookingAsync(
            artistId, customerAuth.UserId, BookingStatus.Completed, completedDaysAgo: 30);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<DailyHealedPhotoPromptJob>();
        await job.RunAsync();

        var customerEmail = await GetUserEmailAsync(customerAuth.UserId);
        _fixture.Emails.Sent.Should().NotContain(s =>
            s.To == customerEmail && s.Subject.Contains("healed", StringComparison.OrdinalIgnoreCase));
    }

    // ---- DailyBookingReminderJob ----

    [Fact]
    public async Task BookingReminder_DispatchesToBothPartiesOnce()
    {
        await ResetEmailsAsync();
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        var bookingId = await SeedActiveBookingAsync(
            artistId, customerAuth.UserId,
            confirmedSessionDate: DateTime.UtcNow.AddHours(24),
            status: BookingStatus.Confirmed);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<DailyBookingReminderJob>();
        await job.RunAsync();
        await job.RunAsync(); // idempotent

        var artistEmail = await GetUserEmailAsync(artistAuth.UserId);
        var customerEmail = await GetUserEmailAsync(customerAuth.UserId);
        _fixture.Emails.Sent.Count(s =>
            s.To == customerEmail && s.Subject.Contains("Reminder", StringComparison.OrdinalIgnoreCase))
            .Should().Be(1);
        _fixture.Emails.Sent.Count(s =>
            s.To == artistEmail && s.Subject.Contains("Reminder", StringComparison.OrdinalIgnoreCase))
            .Should().Be(1);

        await using var verify = _fixture.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        booking.ReminderSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BookingReminder_SkipsBookingsOutsideWindow()
    {
        await ResetEmailsAsync();
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        // 5 days out — outside 12-36h window.
        await SeedActiveBookingAsync(
            artistId, customerAuth.UserId,
            confirmedSessionDate: DateTime.UtcNow.AddDays(5),
            status: BookingStatus.Confirmed);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var job = scope.ServiceProvider.GetRequiredService<DailyBookingReminderJob>();
        await job.RunAsync();

        var customerEmail = await GetUserEmailAsync(customerAuth.UserId);
        _fixture.Emails.Sent.Should().NotContain(s =>
            s.To == customerEmail && s.Subject.Contains("Reminder", StringComparison.OrdinalIgnoreCase));
    }

    // ---- helpers ----

    private async Task<(HttpClient Client, Needlr.Contracts.Auth.AuthResponse Auth, Guid ArtistId)>
        CreateArtistWithStudioAsync()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == auth.UserId);

        var studioId = Guid.NewGuid();
        var studio = new Needlr.Domain.Studios.Studio(
            id: studioId,
            name: $"Studio {studioId:N}",
            studioType: StudioType.Shop,
            location: new NetTopologySuite.Geometries.Point(-73.5674, 45.5019) { SRID = 4326 },
            address: "1 Job Test Ave",
            createdByArtistId: artist.Id,
            joinPolicy: JoinPolicy.Open);
        db.Studios.Add(studio);
        db.ArtistStudioAffiliations.Add(new Needlr.Domain.Studios.ArtistStudioAffiliation(
            id: Guid.NewGuid(),
            artistId: artist.Id,
            studioId: studioId,
            role: AffiliationRole.Founder,
            affiliationType: AffiliationType.Permanent,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            status: AffiliationStatus.Active,
            isPrimary: true));
        artist.PaymentStatus = ArtistPaymentStatus.Active;
        artist.StripeConnectAccountId = $"acct_test_{artist.Id:N}";
        await db.SaveChangesAsync();

        return (client, auth, artist.Id);
    }

    private async Task<Guid> SeedTerminalBookingAsync(
        Guid artistId, Guid customerId, BookingStatus status, int completedDaysAgo)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId)
            .Select(a => a.StudioId)
            .FirstAsync();

        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now.AddDays(-completedDaysAgo - 30),
            requestedDate: DateOnly.FromDateTime(now.AddDays(-completedDaysAgo)),
            estimatedDurationHours: 2m,
            description: "x",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 100m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.Status = status;
        booking.CompletedAt = now.AddDays(-completedDaysAgo);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task<Guid> SeedActiveBookingAsync(
        Guid artistId, Guid customerId, DateTime confirmedSessionDate, BookingStatus status)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId)
            .Select(a => a.StudioId)
            .FirstAsync();

        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now.AddDays(-7),
            requestedDate: DateOnly.FromDateTime(confirmedSessionDate),
            estimatedDurationHours: 2m,
            description: "x",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 100m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.Status = status;
        booking.AcceptedAt = now.AddHours(-1);
        booking.ConfirmedSessionDate = confirmedSessionDate.Kind == DateTimeKind.Utc
            ? confirmedSessionDate
            : DateTime.SpecifyKind(confirmedSessionDate, DateTimeKind.Utc);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task SeedBookingAttachmentAsync(Guid bookingId)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);

        db.BookingAttachments.Add(new Needlr.Domain.Bookings.BookingAttachment(
            id: Guid.NewGuid(),
            bookingId: bookingId,
            messageId: null,
            url: $"reference/{bookingId:N}/file.jpg",
            originalFilename: "ref.jpg",
            mimeType: "image/jpeg",
            sizeBytes: 1024,
            uploadedByUserId: booking.CustomerId,
            uploadedAt: clock.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedArtistCredentialAsync(
        Guid artistId, DateTime issued, DateTime expiry, VerificationStatus status)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var cred = new ArtistCredential(
            id: Guid.NewGuid(),
            artistId: artistId,
            jurisdictionId: MontrealJurisdictionId,
            credentialType: ArtistCredentialType.BloodbornePathogenCertification,
            issuedDate: DateOnly.FromDateTime(issued),
            expiryDate: DateOnly.FromDateTime(expiry),
            documentUrl: $"cred/{Guid.NewGuid():N}");
        cred.VerificationStatus = status;
        if (status == VerificationStatus.Verified)
            cred.VerifiedAt = clock.UtcNow.AddDays(-7);
        db.ArtistCredentials.Add(cred);
        await db.SaveChangesAsync();
        return cred.Id;
    }

    private async Task<string> GetUserEmailAsync(Guid userId)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.Email!).FirstAsync();
    }

    private Task ResetEmailsAsync()
    {
        _fixture.Emails.Sent.Clear();
        _fixture.Pushes.Sent.Clear();
        return Task.CompletedTask;
    }
}

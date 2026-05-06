using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Discovery;
using Needlr.Contracts.Studios;
using Needlr.Contracts.TrustSafety;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.TrustSafety;

public class TrustSafetyEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public TrustSafetyEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    // ---- Feedback ----

    [Fact]
    public async Task SubmitFeedback_HappyPath_PersistsRow()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAsync();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await SeedCompletedBookingAsync(artistId, customerAuth.UserId);

        var resp = await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/feedback",
            new SubmitBookingFeedbackRequest(5, 5, 4, true, "Great session"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitFeedback_NotCompleted_Returns412()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await SeedActiveBookingAsync(
            artistId, customerAuth.UserId, BookingStatus.Confirmed,
            DateTime.UtcNow.AddDays(7));

        var resp = await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/feedback",
            new SubmitBookingFeedbackRequest(5, 5, 5, true, null));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task SubmitFeedback_DuplicateSubmission_Returns409()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await SeedCompletedBookingAsync(artistId, customerAuth.UserId);

        (await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/feedback",
            new SubmitBookingFeedbackRequest(5, 5, 5, true, null))).EnsureSuccessStatusCode();

        var second = await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/feedback",
            new SubmitBookingFeedbackRequest(4, 4, 4, false, null));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitFeedback_NotCustomer_Returns403()
    {
        var (artist, _, _) = await CreateArtistWithStudioAsync();
        var resp = await artist.PostAsJsonAsync($"/api/bookings/{Guid.NewGuid()}/feedback",
            new SubmitBookingFeedbackRequest(5, 5, 5, true, null));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Behavioral signals ----

    [Fact]
    public async Task BehavioralSignals_NullWhenUnderThreshold()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/artists/{artistId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<ArtistDetailResponse>())!;
        body.BehavioralSignals.Should().NotBeNull();
        body.BehavioralSignals.CompletionRatePercent.Should().BeNull();
        body.BehavioralSignals.HealedPhotoRatePercent.Should().BeNull();
        body.BehavioralSignals.RepeatClientRatePercent.Should().BeNull();
    }

    [Fact]
    public async Task BehavioralSignals_CompletionRateComputed_When10Bookings()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();

        // 10 bookings: 7 completed, 3 cancelled-by-customer.
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        for (var i = 0; i < 7; i++)
            await SeedAcceptedBookingAsync(artistId, customerAuth.UserId, finalStatus: BookingStatus.Completed);
        for (var i = 0; i < 3; i++)
            await SeedAcceptedBookingAsync(artistId, customerAuth.UserId, finalStatus: BookingStatus.CancelledByCustomer);

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/artists/{artistId}");
        var body = (await resp.Content.ReadFromJsonAsync<ArtistDetailResponse>())!;
        body.BehavioralSignals.CompletionRatePercent.Should().BeApproximately(70.0, 0.5);
    }

    // ---- Suspension ----

    [Fact]
    public async Task SuspendArtist_HidesFromDiscovery()
    {
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        await SeedVerifiedHealthInspectionForArtistAsync(artistId);

        var anonymous = _fixture.Factory.CreateClient();
        // Sanity-check artist appears.
        var pageBefore = await Search(anonymous);
        pageBefore.Items.Should().Contain(s => s.ActiveArtistCount > 0);

        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        (await admin.PostAsJsonAsync($"/api/admin/users/{artistAuth.UserId}/suspend",
            new SuspendUserRequest("policy violation"))).EnsureSuccessStatusCode();

        var pageAfter = await Search(anonymous);
        pageAfter.Items.Should().NotContain(s => s.ActiveArtistCount > 0
            && _fixture.Factory.Services.GetRequiredService<NeedlrDbContext>()
                .ArtistStudioAffiliations.Any(a => a.StudioId == s.Id && a.ArtistId == artistId));

        var artistDetail = await anonymous.GetAsync($"/api/artists/{artistId}");
        artistDetail.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendArtist_RejectsNewBookingRequests()
    {
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        (await admin.PostAsJsonAsync($"/api/admin/users/{artistAuth.UserId}/suspend",
            new SuspendUserRequest("policy violation"))).EnsureSuccessStatusCode();

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            2m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SuspendCustomer_RejectsNewBookingRequests()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        (await admin.PostAsJsonAsync($"/api/admin/users/{customerAuth.UserId}/suspend",
            new SuspendUserRequest("repeated abuse"))).EnsureSuccessStatusCode();

        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            2m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnsuspendUser_RestoresAccess()
    {
        var (_, artistAuth, artistId) = await CreateArtistWithStudioAsync();
        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        (await admin.PostAsJsonAsync($"/api/admin/users/{artistAuth.UserId}/suspend",
            new SuspendUserRequest("misclick"))).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/admin/users/{artistAuth.UserId}/unsuspend", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/artists/{artistId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- Warnings ----

    [Fact]
    public async Task WarnUser_RecordsAuditRow()
    {
        var (_, _, customerAuth) = await CreateAndAuthCustomerAsync();
        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var warnResp = await admin.PostAsJsonAsync($"/api/admin/users/{customerAuth.UserId}/warn",
            new WarnUserRequest("inappropriate language"));
        warnResp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var rows = await db.UserWarnings.AsNoTracking()
            .Where(w => w.UserId == customerAuth.UserId).ToListAsync();
        rows.Should().ContainSingle();
    }

    // ---- Dashboard ----

    [Fact]
    public async Task TrustSafetyDashboard_FlagsLowAverages_AndSafetyKeywords()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAsync();
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        // Seed 5 feedbacks: all 1/1/1 → average 1.0 (well below 3.0 threshold).
        for (var i = 0; i < 5; i++)
        {
            var bid = await SeedCompletedBookingAsync(artistId, customerAuth.UserId);
            await SeedFeedbackDirectAsync(bid, customerAuth.UserId,
                comm: 1, clean: 1, brief: 1, wouldBookAgain: false,
                freeText: i == 0 ? "felt unsafe" : null);
        }

        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var resp = await admin.GetAsync("/api/admin/trust-safety");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<TrustSafetyDashboardResponse>())!;
        body.LowFeedbackAverages.Should().Contain(a => a.ArtistId == artistId);
        body.RepeatNotBookingAgain.Should().Contain(a => a.ArtistId == artistId);
        body.SafetyKeywordMatches.Should().Contain(f =>
            f.ArtistId == artistId && f.MatchedKeyword.Equals("unsafe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TrustSafetyDashboard_NonAdmin_Returns403()
    {
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await customer.GetAsync("/api/admin/trust-safety");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- helpers ----

    private async Task<DiscoveryPageResponse> Search(HttpClient client)
    {
        const double mtlLat = 45.5019, mtlLng = -73.5674;
        var url = $"/api/discovery/studios?southLat={mtlLat - 0.1}&westLng={mtlLng - 0.1}" +
                  $"&northLat={mtlLat + 0.1}&eastLng={mtlLng + 0.1}" +
                  $"&centerLat={mtlLat}&centerLng={mtlLng}&verifiedOnly=false";
        return (await (await client.GetAsync(url)).Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;
    }

    private async Task<(HttpClient Client, AuthResponse Auth, Guid ArtistId)> CreateArtistWithStudioAsync()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == auth.UserId);

        var studioId = Guid.NewGuid();
        db.Studios.Add(new Needlr.Domain.Studios.Studio(
            id: studioId,
            name: $"Studio {studioId:N}",
            studioType: StudioType.Shop,
            location: new NetTopologySuite.Geometries.Point(-73.5674, 45.5019) { SRID = 4326 },
            address: "1 T&S Test Ave",
            createdByArtistId: artist.Id,
            joinPolicy: JoinPolicy.Open));
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

    private async Task<(HttpClient Client, AuthResponse Auth, AuthResponse customerAuth)>
        CreateAndAuthCustomerAsync()
    {
        var (client, auth) = await AuthHelpers.CreateCustomerClient(_fixture);
        return (client, auth, auth);
    }

    private async Task<Guid> SeedCompletedBookingAsync(Guid artistId, Guid customerId)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId).Select(a => a.StudioId).FirstAsync();

        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now.AddDays(-30),
            requestedDate: DateOnly.FromDateTime(now.AddDays(-7)),
            estimatedDurationHours: 2m,
            description: "x",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 100m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.Status = BookingStatus.Completed;
        booking.AcceptedAt = now.AddDays(-25);
        booking.CompletedAt = now.AddDays(-7);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task<Guid> SeedAcceptedBookingAsync(
        Guid artistId, Guid customerId, BookingStatus finalStatus)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId).Select(a => a.StudioId).FirstAsync();

        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now.AddDays(-30),
            requestedDate: DateOnly.FromDateTime(now.AddDays(-1)),
            estimatedDurationHours: 2m,
            description: "x",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 100m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.AcceptedAt = now.AddDays(-25);
        booking.Status = finalStatus;
        if (finalStatus == BookingStatus.Completed)
            booking.CompletedAt = now.AddDays(-1);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task<Guid> SeedActiveBookingAsync(
        Guid artistId, Guid customerId, BookingStatus status, DateTime confirmedSessionDate)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId).Select(a => a.StudioId).FirstAsync();

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
        booking.ConfirmedSessionDate = DateTime.SpecifyKind(confirmedSessionDate, DateTimeKind.Utc);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task SeedFeedbackDirectAsync(
        Guid bookingId, Guid customerId,
        int comm, int clean, int brief, bool wouldBookAgain, string? freeText)
    {
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        db.BookingFeedbacks.Add(new BookingFeedback(
            id: Guid.NewGuid(),
            bookingId: bookingId,
            customerId: customerId,
            communicationRating: comm,
            cleanlinessRating: clean,
            respectedDesignBriefRating: brief,
            wouldBookAgain: wouldBookAgain,
            submittedAt: clock.UtcNow,
            freeText: freeText));
        await db.SaveChangesAsync();
    }

    private async Task SeedVerifiedHealthInspectionForArtistAsync(Guid artistId)
    {
        var jurisdictionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var studioId = await db.ArtistStudioAffiliations
            .Where(a => a.ArtistId == artistId).Select(a => a.StudioId).FirstAsync();
        var cred = new Needlr.Domain.Verification.StudioCredential(
            id: Guid.NewGuid(),
            studioId: studioId,
            jurisdictionId: jurisdictionId,
            credentialType: StudioCredentialType.HealthInspection,
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
            documentUrl: $"cred/{Guid.NewGuid():N}");
        cred.VerificationStatus = VerificationStatus.Verified;
        cred.VerifiedAt = clock.UtcNow;
        db.StudioCredentials.Add(cred);
        await db.SaveChangesAsync();
    }
}

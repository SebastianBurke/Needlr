using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Studios;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Bookings;

public class BookingEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public BookingEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Request_HappyPath_PersistsRequestedBookingWithStrippedDescription()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        await CreateStudioAsync(artistClient);
        await MarkArtistPaymentActiveAsync(_fixture, artistId);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            ArtistId: artistId,
            BookingType: "TattooSession",
            RequestedDate: future,
            EstimatedDurationHours: 2m,
            Description: "Want a fineline rose. Reach me at jane@example.com or +1 514 555-1212.",
            BodyPlacement: "Forearm",
            CustomerPaymentMethodId: "pm_card_visa",
            ApproximateSizeCm: 8));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        // Detail visible to the customer; description should have stripped contact info.
        var detail = await customer.GetAsync($"/api/bookings/{created.Id}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await detail.Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        body.Status.Should().Be("Requested");
        body.ArtistId.Should().Be(artistId);
        body.Description.Should().NotContain("@example.com");
        body.Description.Should().NotContain("514");
        body.DepositAmountCad.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Request_LeadTimeTooSoon_Returns412()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        await CreateStudioAsync(artistClient);
        await MarkArtistPaymentActiveAsync(_fixture, artistId);

        // Tighten the artist's TattooSession lead time to 14 days.
        (await artistClient.PutAsJsonAsync("/api/availability/lead-times", new SetLeadTimesRequest(
        [
            new LeadTimeRequestItem("Consultation", 3),
            new LeadTimeRequestItem("TattooSession", 14),
            new LeadTimeRequestItem("Touchup", 7),
        ]))).EnsureSuccessStatusCode();

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var tooSoon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            ArtistId: artistId,
            BookingType: "TattooSession",
            RequestedDate: tooSoon,
            EstimatedDurationHours: 1m,
            Description: "x",
            BodyPlacement: "Forearm",
            CustomerPaymentMethodId: "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Request_NotAcceptingBookings_Returns412()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        await CreateStudioAsync(artistClient);
        await MarkArtistPaymentActiveAsync(_fixture, artistId);

        // Flip AcceptingNewBookings off via direct update — there's no API for it in v1 yet.
        await SetAcceptingNewBookingsAsync(_fixture, artistId, false);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", future, 2m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Request_NoActiveStudio_Returns412()
    {
        var (_, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        // Skip studio creation. Artist has no affiliations.
        await MarkArtistPaymentActiveAsync(_fixture, artistId);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", future, 2m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Request_ArtistNotPaymentActive_Returns412()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        await CreateStudioAsync(artistClient);
        // Deliberately skip MarkArtistPaymentActiveAsync — artist remains NotOnboarded.

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", future, 2m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task ArtistAccept_TransitionsToAccepted_AndConsumesAvailability()
    {
        var (artist, artistAuth, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        // 14+ days out so the default 7-day TattooSession lead time isn't a factor.
        var baseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        var requestedDate = NextDayOfWeek(baseDate, DayOfWeek.Tuesday);
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, requestedDate, hours: 3m);

        var sessionDateUtc = requestedDate.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        var accept = await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionDateUtc));
        accept.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        detail.Status.Should().Be("Accepted");
        detail.ConfirmedSessionDate.Should().NotBeNull();

        // Capacity should be consumed: the projection for that day should now show
        // RemainingSessionHours reduced by the booking's hours.
        var anonymous = _fixture.Factory.CreateClient();
        var pj = (await (await anonymous.GetAsync(
                $"/api/availability/artists/{artistId}/projection?from={requestedDate:yyyy-MM-dd}&to={requestedDate:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<ProjectionResponse>())!;
        pj.Days.Single().RemainingSessionHours.Should().BeLessThan(8m);
    }

    [Fact]
    public async Task ArtistAccept_NonRequestedStatus_Returns412()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, hours: 1m);

        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        // Second accept should fail.
        var second = await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc));
        second.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Decline_HappyPath_TransitionsAndStoresReason()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var bookingId = await CreateRequestedBookingAsync(
            customer, artistId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)), 2m);

        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/decline",
            new DeclineBookingRequest("OutsideMyStyle", "Sorry, not my style."))).EnsureSuccessStatusCode();

        var detail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        detail.Status.Should().Be("Declined");
        detail.DeclineReason.Should().Be("OutsideMyStyle");
        detail.DeclineNote.Should().Be("Sorry, not my style.");
    }

    [Fact]
    public async Task RequestInfo_ThenRespond_RoundTripsToRequested()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        (await artist.PostAsync($"/api/bookings/{bookingId}/request-info", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var midDetail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        midDetail.Status.Should().Be("AwaitingCustomerInfo");

        // Customer responds with revised description.
        var newDate = date.AddDays(7);
        (await customer.PostAsJsonAsync($"/api/bookings/{bookingId}/respond-info",
            new RespondWithMoreInfoRequest(
                Description: "Revised: smaller piece, line-only.",
                RequestedDate: newDate,
                EstimatedDurationHours: 1.5m,
                BodyPlacement: "UpperArm",
                ApproximateSizeCm: 6))).EnsureSuccessStatusCode();

        var afterDetail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        afterDetail.Status.Should().Be("Requested");
        afterDetail.Description.Should().Contain("smaller piece");
        afterDetail.RequestedDate.Should().Be(newDate);
        afterDetail.BodyPlacement.Should().Be("UpperArm");
    }

    [Fact]
    public async Task Lifecycle_AcceptedToInProgressToCompleted()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();
        (await artist.PostAsync($"/api/bookings/{bookingId}/in-progress", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await artist.PostAsync($"/api/bookings/{bookingId}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        detail.Status.Should().Be("Completed");
        detail.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CustomerCancel_StrictPolicy_NoRefundInsidePolicy()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        await SetCancellationPolicyAsync(_fixture, artistId, CancellationPolicy.Strict);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var resp = await customer.PostAsync($"/api/bookings/{bookingId}/cancel-customer", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<CancelBookingResponse>())!;
        body.RefundedAmountCad.Should().Be(0m);
    }

    [Fact]
    public async Task CustomerCancel_FlexiblePolicy_FullRefundOutsideWindow()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        await SetCancellationPolicyAsync(_fixture, artistId, CancellationPolicy.Flexible);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        // 15 days out is well past the 48h flexible window — full refund.
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var resp = await customer.PostAsync($"/api/bookings/{bookingId}/cancel-customer", null);
        var body = (await resp.Content.ReadFromJsonAsync<CancelBookingResponse>())!;
        body.RefundedAmountCad.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task ArtistCancel_AlwaysFullRefund()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        await SetCancellationPolicyAsync(_fixture, artistId, CancellationPolicy.Strict);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var resp = await artist.PostAsync($"/api/bookings/{bookingId}/cancel-artist", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<CancelBookingResponse>())!;
        body.RefundedAmountCad.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetDetail_ThirdParty_Returns403()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var (other, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await other.GetAsync($"/api/bookings/{bookingId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListMineAsCustomer_FiltersByStatus()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        await CreateStudioAsync(artistClient);
        await MarkArtistPaymentActiveAsync(_fixture, artistId);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var d = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        await CreateRequestedBookingAsync(customer, artistId, d, 1m);
        await CreateRequestedBookingAsync(customer, artistId, d.AddDays(2), 1m);

        var resp = await customer.GetAsync("/api/bookings/mine/customer?status=Requested&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<BookingPageResponse>())!;
        body.TotalCount.Should().BeGreaterOrEqualTo(2);
        body.Items.Should().AllSatisfy(b => b.Status.Should().Be("Requested"));
    }

    [Fact]
    public async Task ListMineAsArtist_OnlyReturnsTheirBookings()
    {
        var (artistA, artistAauth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistAId = await ResolveArtistIdAsync(_fixture, artistAauth.UserId);
        await CreateStudioAsync(artistA);
        await MarkArtistPaymentActiveAsync(_fixture, artistAId);

        var (artistB, _) = await AuthHelpers.CreateArtistClient(_fixture);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var d = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        await CreateRequestedBookingAsync(customer, artistAId, d, 1m);

        var artistAList = (await (await artistA.GetAsync("/api/bookings/mine/artist"))
            .Content.ReadFromJsonAsync<BookingPageResponse>())!;
        artistAList.Items.Should().NotBeEmpty();

        var artistBList = (await (await artistB.GetAsync("/api/bookings/mine/artist"))
            .Content.ReadFromJsonAsync<BookingPageResponse>())!;
        artistBList.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task NonCustomerCannotRequest_NonArtistCannotAccept()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await artistClient.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            Guid.NewGuid(), "TattooSession",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), 1m, "x", "Forearm", "pm_card_visa"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var accept = await customer.PostAsJsonAsync($"/api/bookings/{Guid.NewGuid()}/accept",
            new AcceptBookingRequest(DateTime.UtcNow.AddDays(20)));
        accept.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- helpers ---

    private async Task<(HttpClient Client, Needlr.Contracts.Auth.AuthResponse Auth, Guid ArtistId)>
        CreateArtistWithStudioAndPattern()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        await CreateStudioAsync(client);
        await MarkArtistPaymentActiveAsync(_fixture, artistId);
        // 8h Available every weekday, Sunday Closed.
        var weekly = new SetAvailabilityPatternRequest(
        [
            new("Monday",    "Available", 8m, null, null),
            new("Tuesday",   "Available", 8m, null, null),
            new("Wednesday", "Available", 8m, null, null),
            new("Thursday",  "Available", 8m, null, null),
            new("Friday",    "Available", 8m, null, null),
            new("Saturday",  "Available", 8m, null, null),
            new("Sunday",    "Closed",    null, null, null),
        ]);
        (await client.PutAsJsonAsync("/api/availability/pattern", weekly)).EnsureSuccessStatusCode();
        return (client, auth, artistId);
    }

    private static async Task<Guid> CreateStudioAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Booking Test Ave",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> CreateRequestedBookingAsync(
        HttpClient customerClient, Guid artistId, DateOnly requestedDate, decimal hours)
    {
        var resp = await customerClient.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            ArtistId: artistId,
            BookingType: "TattooSession",
            RequestedDate: requestedDate,
            EstimatedDurationHours: hours,
            Description: "Test description.",
            BodyPlacement: "Forearm",
            CustomerPaymentMethodId: "pm_card_visa"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    /// <summary>
    /// Marks the artist as fully Stripe-onboarded so booking-request preconditions pass.
    /// Real onboarding goes through CreateConnectAccountCommand + the Stripe webhook in
    /// production; here we shortcut directly via DbContext.
    /// </summary>
    private static async Task MarkArtistPaymentActiveAsync(WebAppFixture fixture, Guid artistId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.StripeConnectAccountId ??= $"acct_test_{artist.Id:N}";
        artist.PaymentStatus = ArtistPaymentStatus.Active;
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task SetAcceptingNewBookingsAsync(WebAppFixture fixture, Guid artistId, bool accepting)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.AcceptingNewBookings = accepting;
        await db.SaveChangesAsync();
    }

    private static async Task SetCancellationPolicyAsync(WebAppFixture fixture, Guid artistId, CancellationPolicy policy)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.CancellationPolicy = policy;
        await db.SaveChangesAsync();
    }

    private static DateOnly NextDayOfWeek(DateOnly from, DayOfWeek target)
    {
        var delta = ((int)target - (int)from.DayOfWeek + 7) % 7;
        if (delta == 0) delta = 7;
        return from.AddDays(delta);
    }
}

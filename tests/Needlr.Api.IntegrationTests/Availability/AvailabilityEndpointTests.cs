using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Studios;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Availability;

public class AvailabilityEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public AvailabilityEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SetPattern_ReplacesExistingRows_AndPopulatesProjection()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        var firstRequest = new SetAvailabilityPatternRequest(
        [
            new AvailabilityPatternDayRequest("Monday", "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Tuesday", "Limited", 4m, null, null),
            new AvailabilityPatternDayRequest("Sunday", "Closed", null, null, null),
        ]);
        var firstResp = await client.PutAsJsonAsync("/api/availability/pattern", firstRequest);
        firstResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var firstGet = await client.GetAsync("/api/availability/pattern");
        var firstBody = (await firstGet.Content.ReadFromJsonAsync<AvailabilityPatternResponse>())!;
        firstBody.Days.Should().HaveCount(3);
        firstBody.Days.Should().Contain(d => d.DayOfWeek == "Monday" && d.Status == "Available");

        // Replace with a different set; the prior rows should be gone.
        var secondRequest = new SetAvailabilityPatternRequest(
        [
            new AvailabilityPatternDayRequest("Wednesday", "Available", 6m, null, null),
        ]);
        (await client.PutAsJsonAsync("/api/availability/pattern", secondRequest))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondGet = await client.GetAsync("/api/availability/pattern");
        var secondBody = (await secondGet.Content.ReadFromJsonAsync<AvailabilityPatternResponse>())!;
        secondBody.Days.Should().ContainSingle(d => d.DayOfWeek == "Wednesday");

        // Projection should now have 90 rows (the rolling window).
        var projectionRowCount = await CountProjectionRowsAsync(_fixture, artistId);
        projectionRowCount.Should().Be(90);
    }

    [Fact]
    public async Task SetPattern_DuplicateDayOfWeek_Returns400()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var bad = new SetAvailabilityPatternRequest(
        [
            new AvailabilityPatternDayRequest("Monday", "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Monday", "Limited", 4m, null, null),
        ]);
        var resp = await client.PutAsJsonAsync("/api/availability/pattern", bad);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProjectionReflectsPattern_AvailableDayBookable_ClosedDayNot()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        await SetWeeklyOpenPatternAsync(client, status: "Available", maxHours: 8m);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstSunday = NextDayOfWeek(today, DayOfWeek.Sunday);

        // Pattern only set on Mon-Sat. Add an explicit Closed Sunday so we can assert.
        var closedSunday = new SetAvailabilityPatternRequest(
            BuildWeeklyDays(weekday: "Available", sunday: "Closed", maxHours: 8m));
        (await client.PutAsJsonAsync("/api/availability/pattern", closedSunday))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = _fixture.Factory.CreateClient();
        var url = $"/api/availability/artists/{artistId}/projection?from={today:yyyy-MM-dd}&to={today.AddDays(13):yyyy-MM-dd}";
        var resp = await anonymous.GetAsync(url);
        var body = (await resp.Content.ReadFromJsonAsync<ProjectionResponse>())!;

        body.Days.Should().HaveCount(14);
        var anyMondayRow = body.Days.FirstOrDefault(d => d.Date.DayOfWeek == DayOfWeek.Monday);
        anyMondayRow.Should().NotBeNull();
        anyMondayRow!.IsBookable.Should().BeTrue();

        var sundayRow = body.Days.First(d => d.Date == firstSunday);
        sundayRow.IsBookable.Should().BeFalse();
        sundayRow.RemainingSessionHours.Should().Be(0m);
    }

    [Fact]
    public async Task Override_Closes_OtherwiseBookableDay()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        await SetWeeklyOpenPatternAsync(client, "Available", 8m);

        // Find a near-future date that's bookable per pattern, then override it Closed.
        var anonymous = _fixture.Factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDate = NextDayOfWeek(today, DayOfWeek.Wednesday);

        var preResp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        var preDay = (await preResp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single();
        preDay.IsBookable.Should().BeTrue();

        // Override that Wednesday to Closed.
        var addOverride = new AddAvailabilityOverrideRequest(targetDate, "Closed", null, "On vacation");
        (await client.PostAsJsonAsync("/api/availability/overrides", addOverride))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var postResp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        var postDay = (await postResp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single();
        postDay.IsBookable.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveOverride_RestoresPatternBookability()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        await SetWeeklyOpenPatternAsync(client, "Available", 8m);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDate = NextDayOfWeek(today, DayOfWeek.Thursday);

        await client.PostAsJsonAsync("/api/availability/overrides",
            new AddAvailabilityOverrideRequest(targetDate, "Closed", null, null));

        var anonymous = _fixture.Factory.CreateClient();
        var midResp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        (await midResp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single().IsBookable.Should().BeFalse();

        // Now remove it.
        var del = await client.DeleteAsync($"/api/availability/overrides/{targetDate:yyyy-MM-dd}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var postResp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        (await postResp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single().IsBookable.Should().BeTrue();
    }

    [Fact]
    public async Task Booking_Consumes_DayCapacity()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        var studioId = await CreateStudioAsync(client);

        await SetWeeklyOpenPatternAsync(client, "Available", maxHours: 4m);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDate = NextDayOfWeek(today, DayOfWeek.Friday);

        // Insert a Confirmed 3-hour booking on the target date directly.
        await SeedConfirmedBookingAsync(_fixture, artistId, studioId, targetDate, hours: 3m);

        // Force a rebuild via admin to pick up the booking (change commands rebuild on save,
        // but this booking was inserted out-of-band).
        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        (await admin.PostAsync($"/api/availability/projection/rebuild/{artistId}", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        var day = (await resp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single();
        day.RemainingSessionHours.Should().Be(1m);
        day.IsBookable.Should().BeTrue();

        // Add a second 1-hour booking that drains the day.
        await SeedConfirmedBookingAsync(_fixture, artistId, studioId, targetDate, hours: 1m);
        (await admin.PostAsync($"/api/availability/projection/rebuild/{artistId}", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var resp2 = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={targetDate:yyyy-MM-dd}&to={targetDate:yyyy-MM-dd}");
        var day2 = (await resp2.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single();
        day2.RemainingSessionHours.Should().Be(0m);
        day2.IsBookable.Should().BeFalse();
    }

    [Fact]
    public async Task ClosedBookingWindow_GatesBookability()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        await SetWeeklyOpenPatternAsync(client, "Available", 8m);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Window not yet open: opens tomorrow, closes day after — but pattern says today is Available.
        var futureWindow = new CreateBookingWindowRequest(
            WindowOpensAt: DateTime.UtcNow.AddDays(1),
            WindowClosesAt: DateTime.UtcNow.AddDays(2),
            TargetRangeStart: today.AddDays(7),
            TargetRangeEnd: today.AddDays(14));
        var createWin = await client.PostAsJsonAsync("/api/availability/windows", futureWindow);
        createWin.StatusCode.Should().Be(HttpStatusCode.OK);

        var anonymous = _fixture.Factory.CreateClient();
        var probeDate = NextDayOfWeek(today, DayOfWeek.Monday);
        var resp = await anonymous.GetAsync(
            $"/api/availability/artists/{artistId}/projection?from={probeDate:yyyy-MM-dd}&to={probeDate:yyyy-MM-dd}");
        var day = (await resp.Content.ReadFromJsonAsync<ProjectionResponse>())!.Days.Single();
        // With any window present, dates not covered by an open window are not bookable.
        day.IsBookable.Should().BeFalse();
    }

    [Fact]
    public async Task LeadTimes_Set_And_Retrieve_RoundTrip()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var setReq = new SetLeadTimesRequest(
        [
            new LeadTimeRequestItem("Consultation", 3),
            new LeadTimeRequestItem("TattooSession", 14),
            new LeadTimeRequestItem("Touchup", 7),
        ]);
        (await client.PutAsJsonAsync("/api/availability/lead-times", setReq))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await client.GetAsync("/api/availability/lead-times");
        var body = (await getResp.Content.ReadFromJsonAsync<LeadTimesResponse>())!;
        body.Items.Should().HaveCount(3);
        body.Items.Should().Contain(i => i.BookingType == "TattooSession" && i.MinimumDays == 14);
    }

    [Fact]
    public async Task LeadTimes_DuplicateBookingType_Returns400()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var setReq = new SetLeadTimesRequest(
        [
            new LeadTimeRequestItem("Consultation", 3),
            new LeadTimeRequestItem("Consultation", 5),
        ]);
        (await client.PutAsJsonAsync("/api/availability/lead-times", setReq))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ical_Rotate_And_Fetch_Returns_Calendar()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        var studioId = await CreateStudioAsync(client);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedConfirmedBookingAsync(_fixture, artistId, studioId, today.AddDays(3), hours: 2m);

        var rotateResp = await client.PostAsync("/api/availability/ical/rotate", content: null);
        rotateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotate = (await rotateResp.Content.ReadFromJsonAsync<IcalFeedResponse>())!;
        rotate.Token.Should().NotBeNullOrWhiteSpace();
        rotate.FeedUrl.Should().Contain($"/api/availability/ical/{artistId}/");

        var anonymous = _fixture.Factory.CreateClient();
        var feedResp = await anonymous.GetAsync($"/api/availability/ical/{artistId}/{rotate.Token}.ics");
        feedResp.StatusCode.Should().Be(HttpStatusCode.OK);
        feedResp.Content.Headers.ContentType!.MediaType.Should().Be("text/calendar");
        var body = await feedResp.Content.ReadAsStringAsync();
        body.Should().Contain("BEGIN:VCALENDAR");
        body.Should().Contain("END:VCALENDAR");
        body.Should().Contain("BEGIN:VEVENT");
        body.Should().Contain("Booking · TattooSession");
    }

    [Fact]
    public async Task Ical_WrongToken_Returns404()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        var rotate = await client.PostAsync("/api/availability/ical/rotate", content: null);
        rotate.EnsureSuccessStatusCode();

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/availability/ical/{artistId}/wrong-token.ics");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ical_RotateInvalidatesPriorToken()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        var first = (await (await client.PostAsync("/api/availability/ical/rotate", content: null))
            .Content.ReadFromJsonAsync<IcalFeedResponse>())!;
        var second = (await (await client.PostAsync("/api/availability/ical/rotate", content: null))
            .Content.ReadFromJsonAsync<IcalFeedResponse>())!;

        first.Token.Should().NotBe(second.Token);

        var anonymous = _fixture.Factory.CreateClient();
        (await anonymous.GetAsync($"/api/availability/ical/{artistId}/{first.Token}.ics"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await anonymous.GetAsync($"/api/availability/ical/{artistId}/{second.Token}.ics"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pattern_NonArtistCaller_Returns403()
    {
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await customer.GetAsync("/api/availability/pattern");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RebuildArtistProjection_NonAdmin_Returns403()
    {
        var (artist, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await artist.PostAsync($"/api/availability/projection/rebuild/{Guid.NewGuid()}", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- helpers ---

    private static async Task SetWeeklyOpenPatternAsync(HttpClient client, string status, decimal maxHours)
    {
        var req = new SetAvailabilityPatternRequest(BuildWeeklyDays(weekday: status, sunday: "Closed", maxHours));
        var resp = await client.PutAsJsonAsync("/api/availability/pattern", req);
        resp.EnsureSuccessStatusCode();
    }

    private static List<AvailabilityPatternDayRequest> BuildWeeklyDays(string weekday, string sunday, decimal maxHours)
    {
        decimal? hours = weekday == "Closed" ? null : maxHours;
        return new List<AvailabilityPatternDayRequest>
        {
            new("Monday",    weekday, hours, null, null),
            new("Tuesday",   weekday, hours, null, null),
            new("Wednesday", weekday, hours, null, null),
            new("Thursday",  weekday, hours, null, null),
            new("Friday",    weekday, hours, null, null),
            new("Saturday",  weekday, hours, null, null),
            new("Sunday",    sunday,  null,  null, null),
        };
    }

    private static DateOnly NextDayOfWeek(DateOnly from, DayOfWeek target)
    {
        var delta = ((int)target - (int)from.DayOfWeek + 7) % 7;
        if (delta == 0) delta = 7; // tomorrow-or-later, never the same day
        return from.AddDays(delta);
    }

    private static async Task<Guid> CreateStudioAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Test Ave",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task<int> CountProjectionRowsAsync(WebAppFixture fixture, Guid artistId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.ArtistAvailabilityProjections.CountAsync(p => p.ArtistId == artistId);
    }

    private async Task SeedConfirmedBookingAsync(
        WebAppFixture fixture, Guid artistId, Guid studioId, DateOnly date, decimal hours)
    {
        // Register a real customer up front because Booking.CustomerId has an FK to ApplicationUser.
        var (_, customerAuth) = await AuthHelpers.CreateCustomerClient(fixture);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerAuth.UserId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now,
            requestedDate: date,
            estimatedDurationHours: hours,
            description: "Test booking for projection.",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 50m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedSessionDate = date.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);
        booking.AcceptedAt = now;
        booking.DepositCapturedAt = now;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
    }
}

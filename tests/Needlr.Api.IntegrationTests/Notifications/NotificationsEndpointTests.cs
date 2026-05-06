using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Notifications;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Notifications;

public class NotificationsEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public NotificationsEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Preferences_DefaultsToAllOn_WhenNoOverrides()
    {
        var (client, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await client.GetAsync("/api/notifications/preferences");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<NotificationPreferencesResponse>())!;
        body.Items.Should().AllSatisfy(i => i.EmailEnabled.Should().BeTrue());
        body.Items.Should().AllSatisfy(i => i.PushEnabled.Should().BeTrue());
        body.Items.Should().HaveCount(Enum.GetValues<NotificationType>().Length);
    }

    [Fact]
    public async Task UpdatePreferences_PersistsOverrides()
    {
        var (client, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var put = await client.PutAsJsonAsync("/api/notifications/preferences",
            new UpdateNotificationPreferencesRequest(
            [
                new NotificationPreferenceRequestItem("BookingAccepted", false, false),
                new NotificationPreferenceRequestItem("NewMessage", true, false),
            ]));
        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var prefs = (await (await client.GetAsync("/api/notifications/preferences"))
            .Content.ReadFromJsonAsync<NotificationPreferencesResponse>())!;
        var accepted = prefs.Items.Single(i => i.Type == "BookingAccepted");
        accepted.EmailEnabled.Should().BeFalse();
        accepted.PushEnabled.Should().BeFalse();
        var newMessage = prefs.Items.Single(i => i.Type == "NewMessage");
        newMessage.PushEnabled.Should().BeFalse();
        newMessage.EmailEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task PushSubscription_RegisterAndUnregister_RoundTrips()
    {
        var (client, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var endpoint = $"https://push.example/{Guid.NewGuid():N}";

        var register = await client.PostAsJsonAsync("/api/notifications/push-subscriptions",
            new RegisterPushSubscriptionRequest(endpoint, "p256dh-key", "auth-key"));
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await register.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var unregister = await client.DeleteAsync($"/api/notifications/push-subscriptions/{created.Id}");
        unregister.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PushSubscription_RegisterTwice_SameEndpoint_RefreshesInPlace()
    {
        var (client, auth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var endpoint = $"https://push.example/{Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync("/api/notifications/push-subscriptions",
            new RegisterPushSubscriptionRequest(endpoint, "k1", "a1"));
        var firstId = (await first.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var second = await client.PostAsJsonAsync("/api/notifications/push-subscriptions",
            new RegisterPushSubscriptionRequest(endpoint, "k2-rotated", "a2-rotated"));
        var secondId = (await second.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        secondId.Should().Be(firstId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var subs = await db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == auth.UserId)
            .ToListAsync();
        subs.Should().ContainSingle()
            .Which.P256dh.Should().Be("k2-rotated");
    }

    [Fact]
    public async Task BookingAccepted_DispatchesEmail_ToCustomer()
    {
        await ResetRecordingsAsync();

        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var customerEmail = await GetUserEmailAsync(_fixture, customerAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s =>
            s.To == customerEmail && s.Subject.Contains("accepted", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BookingAccepted_PrefOff_SkipsEmail()
    {
        await ResetRecordingsAsync();

        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        // Customer toggles email off for BookingAccepted.
        (await customer.PutAsJsonAsync("/api/notifications/preferences",
            new UpdateNotificationPreferencesRequest(
            [
                new NotificationPreferenceRequestItem("BookingAccepted", false, false),
            ]))).EnsureSuccessStatusCode();

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var customerEmail = await GetUserEmailAsync(_fixture, customerAuth.UserId);
        _fixture.Emails.Sent.Should().NotContain(s =>
            s.To == customerEmail && s.Subject.Contains("accepted", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NewBookingRequest_DispatchesToArtist()
    {
        await ResetRecordingsAsync();

        var (_, artistAuth, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var artistEmail = await GetUserEmailAsync(_fixture, artistAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s =>
            s.To == artistEmail && s.Subject.Contains("booking request", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BookingDeclined_DispatchesToCustomer()
    {
        await ResetRecordingsAsync();

        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/decline",
            new DeclineBookingRequest("OutsideMyStyle", null))).EnsureSuccessStatusCode();

        var customerEmail = await GetUserEmailAsync(_fixture, customerAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s =>
            s.To == customerEmail && s.Subject.Contains("declined", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NewMessage_DispatchesToOtherPartyOnly()
    {
        await ResetRecordingsAsync();

        var (artist, artistAuth, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        // Trigger payment_intent.succeeded webhook to open the thread.
        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        await FirePaymentSucceededWebhookAsync(booking.StripePaymentIntentId!);
        var threadId = await GetThreadIdForBookingAsync(_fixture, bookingId);

        await ResetRecordingsAsync(); // ignore prior dispatches

        // Customer sends; artist should receive notification.
        await customer.PostAsJsonAsync($"/api/threads/{threadId}/messages",
            new Contracts.Messaging.SendMessageRequest("hi"));

        var artistEmail = await GetUserEmailAsync(_fixture, artistAuth.UserId);
        var customerEmail = await GetUserEmailAsync(_fixture, customerAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s => s.To == artistEmail);
        _fixture.Emails.Sent.Should().NotContain(s => s.To == customerEmail);
    }

    [Fact]
    public async Task PushDispatched_WhenSubscriptionRegistered()
    {
        await ResetRecordingsAsync();

        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        // Customer registers a push subscription.
        var endpoint = $"https://push.example/{Guid.NewGuid():N}";
        (await customer.PostAsJsonAsync("/api/notifications/push-subscriptions",
            new RegisterPushSubscriptionRequest(endpoint, "p", "a"))).EnsureSuccessStatusCode();

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        _fixture.Pushes.Sent.Should().Contain(p => p.Endpoint == endpoint);
    }

    // --- helpers ---

    private async Task<(HttpClient Client, AuthResponse Auth, Guid ArtistId)>
        CreateArtistWithStudioAndPattern()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == auth.UserId);
        artist.StripeConnectAccountId = $"acct_test_{artist.Id:N}";
        artist.PaymentStatus = ArtistPaymentStatus.Active;
        await db.SaveChangesAsync();

        (await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Notif Test Ave",
            JoinPolicy: "Open"))).EnsureSuccessStatusCode();

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
        return (client, auth, artist.Id);
    }

    private static async Task<Guid> CreateRequestedBookingAsync(
        HttpClient customer, Guid artistId, DateOnly date, decimal hours)
    {
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", date, hours, "Test description.",
            "Forearm", "pm_card_visa"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Needlr.Domain.Bookings.Booking> GetBookingDirectAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
    }

    private static async Task<Guid> GetThreadIdForBookingAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.AsNoTracking().FirstAsync(t => t.BookingId == bookingId);
        return thread.Id;
    }

    private static async Task<string> GetUserEmailAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email!).FirstAsync();
    }

    private async Task FirePaymentSucceededWebhookAsync(string paymentIntentId)
    {
        var payload = $$"""
        {
            "id":"evt_test_{{Guid.NewGuid():N}}",
            "type":"payment_intent.succeeded",
            "data": {
                "object": {
                    "id":"{{paymentIntentId}}",
                    "object":"payment_intent",
                    "status":"succeeded"
                }
            }
        }
        """;
        var anonymous = _fixture.Factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("Stripe-Signature",
            StripeSignatureHelper.Sign(payload, WebAppFixture.TestStripeWebhookSecret));
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var resp = await anonymous.PostAsync("/api/webhooks/stripe", content);
        resp.EnsureSuccessStatusCode();
    }

    private Task ResetRecordingsAsync()
    {
        _fixture.Emails.Sent.Clear();
        _fixture.Pushes.Sent.Clear();
        return Task.CompletedTask;
    }
}

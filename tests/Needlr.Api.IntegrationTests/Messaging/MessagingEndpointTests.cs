using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Studios;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Messaging;

public class MessagingEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public MessagingEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Send_BeforeDepositCaptured_NoThreadOpen_Returns404()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        // No webhook fired yet — thread should not exist.
        var bookingThreadId = await GetThreadIdForBookingOrNullAsync(_fixture, bookingId);
        bookingThreadId.Should().BeNull();

        // Sending against a fictional thread id returns NotFound (the gate is the missing thread).
        var resp = await customer.PostAsJsonAsync(
            $"/api/threads/{Guid.NewGuid()}/messages",
            new SendMessageRequest("Hi"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DepositCapturedWebhook_OpensThread_AndPartiesCanSendMessages()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        await FirePaymentSucceededWebhookAsync(booking.StripePaymentIntentId!);

        var threadId = await GetThreadIdForBookingOrNullAsync(_fixture, bookingId);
        threadId.Should().NotBeNull();

        // Customer sends, artist sees it; artist replies, customer sees it.
        (await customer.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages",
            new SendMessageRequest("Hi, looking forward!"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages",
            new SendMessageRequest("See you then."))).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = (await (await customer.GetAsync($"/api/threads/{threadId}/messages"))
            .Content.ReadFromJsonAsync<MessagePageResponse>())!;
        page.Items.Should().HaveCount(2);
        page.Items.Select(m => m.Body).Should().BeEquivalentTo(new[] { "Hi, looking forward!", "See you then." });
    }

    [Fact]
    public async Task Send_NonParty_Returns403()
    {
        var threadId = await OpenThreadForFreshBookingAsync();
        var (other, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await other.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("hey"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Send_ToLockedThread_ByParty_Returns412()
    {
        // Use the thread's actual customer so the party check passes — the locked-thread
        // precondition is what we want to verify.
        var (customer, _, threadId) = await OpenThreadAndGetClientsAsync();
        await LockThreadDirectAsync(_fixture, threadId);

        var resp = await customer.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("late ping"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task MarkRead_OnlyRecipient_Allowed()
    {
        var (customer, artist, threadId) = await OpenThreadAndGetClientsAsync();

        // Artist sends; customer marks read.
        var sendResp = await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("hello"));
        var messageId = (await sendResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // Sender marking own message → 412.
        (await artist.PostAsync($"/api/messages/{messageId}/read", null))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        // Recipient marking → 204 + ReadAt set.
        (await customer.PostAsync($"/api/messages/{messageId}/read", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var page = (await (await customer.GetAsync($"/api/threads/{threadId}/messages"))
            .Content.ReadFromJsonAsync<MessagePageResponse>())!;
        page.Items.Single().ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UnreadCount_ReflectsOnlyOtherPartySent()
    {
        var (customer, artist, threadId) = await OpenThreadAndGetClientsAsync();

        await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("a"));
        await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("b"));
        await customer.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("self"));

        var customerCount = (await (await customer.GetAsync("/api/messages/unread-count"))
            .Content.ReadFromJsonAsync<UnreadCountResponse>())!.Count;
        customerCount.Should().Be(2);

        var artistCount = (await (await artist.GetAsync("/api/messages/unread-count"))
            .Content.ReadFromJsonAsync<UnreadCountResponse>())!.Count;
        artistCount.Should().Be(1);
    }

    [Fact]
    public async Task Report_FlagsMessage_AndCreatesReportRow()
    {
        var (customer, artist, threadId) = await OpenThreadAndGetClientsAsync();
        var sendResp = await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("body"));
        var messageId = (await sendResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var reportResp = await customer.PostAsJsonAsync(
            $"/api/messages/{messageId}/report",
            new ReportMessageRequest("Harassment", "uncomfortable"));
        reportResp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var msg = await db.Messages.FirstAsync(m => m.Id == messageId);
        msg.IsReportedFlag.Should().BeTrue();
        var report = await db.MessageReports.FirstAsync(r => r.MessageId == messageId);
        report.Reason.Should().Be(MessageReportReason.Harassment);
    }

    [Fact]
    public async Task Admin_HideMessage_RedactsBody()
    {
        var (customer, artist, threadId) = await OpenThreadAndGetClientsAsync();
        var sendResp = await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("inappropriate text"));
        var messageId = (await sendResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var hideResp = await admin.PostAsJsonAsync(
            $"/api/admin/messages/{messageId}/hide", new HideMessageRequest("violates content policy"));
        hideResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var page = (await (await customer.GetAsync($"/api/threads/{threadId}/messages"))
            .Content.ReadFromJsonAsync<MessagePageResponse>())!;
        page.Items.Single().Body.Should().StartWith("[message hidden by admin");
    }

    [Fact]
    public async Task Admin_ResolveReport_RecordsResolution()
    {
        var (customer, artist, threadId) = await OpenThreadAndGetClientsAsync();
        var sendResp = await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages", new SendMessageRequest("body"));
        var messageId = (await sendResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
        var reportResp = await customer.PostAsJsonAsync(
            $"/api/messages/{messageId}/report",
            new ReportMessageRequest("Spam", null));
        var reportId = (await reportResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var (admin, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var resolveResp = await admin.PostAsJsonAsync(
            $"/api/admin/message-reports/{reportId}/resolve",
            new ResolveReportRequest("MessageHidden"));
        resolveResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var report = await db.MessageReports.FirstAsync(r => r.Id == reportId);
        report.Resolution.Should().Be(MessageReportResolution.MessageHidden);
        report.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LockMessageThread_Idempotent()
    {
        var threadId = await OpenThreadForFreshBookingAsync();
        var bookingId = await GetBookingIdForThreadAsync(_fixture, threadId);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var first = await mediator.Send(
            new Needlr.Application.MessageThreads.LockMessageThread.LockMessageThreadCommand(bookingId));
        first.IsSuccess.Should().BeTrue();
        var second = await mediator.Send(
            new Needlr.Application.MessageThreads.LockMessageThread.LockMessageThreadCommand(bookingId));
        second.IsSuccess.Should().BeTrue();

        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.FirstAsync(t => t.Id == threadId);
        thread.Status.Should().Be(MessageThreadStatus.Locked);
        thread.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ListMyThreads_ReturnsPartyThreadsOnly()
    {
        var threadId1 = await OpenThreadForFreshBookingAsync();
        // A second thread for a different customer — not visible to the first customer.
        await OpenThreadForFreshBookingAsync();

        // Resolve the first customer's client by inspecting the thread → booking → customer.
        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.AsNoTracking().FirstAsync(t => t.Id == threadId1);
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == thread.BookingId);
        var customerEmail = await db.Users.AsNoTracking()
            .Where(u => u.Id == booking.CustomerId).Select(u => u.Email).FirstAsync();

        var loginClient = _fixture.Factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/auth/login",
            new Needlr.Contracts.Auth.LoginRequest(customerEmail!, "Strong-Pass-1234"));
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<Needlr.Contracts.Auth.AuthResponse>())!;
        var customer = _fixture.Factory.CreateClient();
        customer.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var resp = await customer.GetAsync("/api/threads/mine");
        var body = (await resp.Content.ReadFromJsonAsync<ThreadPageResponse>())!;
        body.Items.Should().ContainSingle(t => t.Id == threadId1);
    }

    // --- helpers ---

    private async Task<(HttpClient Client, Needlr.Contracts.Auth.AuthResponse Auth, Guid ArtistId)>
        CreateArtistWithStudioAndPattern()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        (await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Msg Test Ave",
            JoinPolicy: "Open"))).EnsureSuccessStatusCode();

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.StripeConnectAccountId = $"acct_test_{artist.Id:N}";
        artist.PaymentStatus = ArtistPaymentStatus.Active;
        await db.SaveChangesAsync();

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

    private static async Task<Guid> CreateRequestedBookingAsync(
        HttpClient customer, Guid artistId, DateOnly date, decimal hours)
    {
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", date, hours, "Test description.",
            "Forearm", "pm_card_visa"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task<Booking> GetBookingDirectAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
    }

    private static async Task<Guid?> GetThreadIdForBookingOrNullAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.AsNoTracking()
            .FirstOrDefaultAsync(t => t.BookingId == bookingId);
        return thread?.Id;
    }

    private static async Task<Guid> GetBookingIdForThreadAsync(WebAppFixture fixture, Guid threadId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.AsNoTracking().FirstAsync(t => t.Id == threadId);
        return thread.BookingId;
    }

    private static async Task LockThreadDirectAsync(WebAppFixture fixture, Guid threadId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var thread = await db.MessageThreads.FirstAsync(t => t.Id == threadId);
        thread.Status = MessageThreadStatus.Locked;
        thread.LockedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> OpenThreadForFreshBookingAsync()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();
        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        await FirePaymentSucceededWebhookAsync(booking.StripePaymentIntentId!);
        return (await GetThreadIdForBookingOrNullAsync(_fixture, bookingId))!.Value;
    }

    private async Task<(HttpClient Customer, HttpClient Artist, Guid ThreadId)> OpenThreadAndGetClientsAsync()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();
        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        await FirePaymentSucceededWebhookAsync(booking.StripePaymentIntentId!);
        var threadId = (await GetThreadIdForBookingOrNullAsync(_fixture, bookingId))!.Value;
        return (customer, artist, threadId);
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
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var resp = await anonymous.PostAsync("/api/webhooks/stripe", content);
        resp.EnsureSuccessStatusCode();
    }
}

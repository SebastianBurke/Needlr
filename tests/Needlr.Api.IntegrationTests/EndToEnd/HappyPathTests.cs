using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Studios;
using Needlr.Contracts.TrustSafety;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.EndToEnd;

/// <summary>
/// Phase 23 hardening — one stitched test that exercises the full v1 pipeline end-to-end:
/// customer signs up, artist signs up + studio + Stripe-active, customer requests, artist
/// accepts, payment_intent.succeeded webhook opens the thread, both parties exchange
/// messages, artist marks Completed, customer leaves feedback. This is the smoke test that
/// proves the parts compose; the per-phase suites cover the edges.
/// </summary>
public class HappyPathTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public HappyPathTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FullFlow_CustomerBookingThroughCompletionAndFeedback()
    {
        // 1. Customer signs up.
        var (customer, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);

        // 2. Artist signs up + creates a studio + flips Stripe-active + sets pattern.
        var (artist, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        var studioId = await CreateStudioAsync(artist);
        await MarkPaymentActiveAsync(_fixture, artistId);
        await SetWeeklyOpenPatternAsync(artist);

        // 3. Customer submits a booking request 21 days out.
        var sessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21));
        var bookingResp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            ArtistId: artistId,
            BookingType: "TattooSession",
            RequestedDate: sessionDate,
            EstimatedDurationHours: 2m,
            Description: "Fineline forearm piece, ~10cm.",
            BodyPlacement: "Forearm",
            CustomerPaymentMethodId: "pm_card_visa",
            ApproximateSizeCm: 10));
        bookingResp.EnsureSuccessStatusCode();
        var bookingId = (await bookingResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // 4. Artist accepts. Webhook fires payment_intent.succeeded → DepositCaptured →
        // Confirmed + opens the thread.
        var sessionUtc = sessionDate.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();
        var paymentIntentId = await GetStripeIntentIdAsync(_fixture, bookingId);
        await FirePaymentSucceededWebhookAsync(paymentIntentId);

        var afterCapture = await GetBookingDetailAsync(customer, bookingId);
        afterCapture.Status.Should().Be("Confirmed");
        afterCapture.DepositCapturedAt.Should().NotBeNull();

        // 5. Thread opened — both parties can exchange messages.
        var threadId = await GetThreadIdAsync(_fixture, bookingId);
        var customerMsg = await customer.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages",
            new SendMessageRequest("Looking forward to it. Anything I should bring?"));
        customerMsg.EnsureSuccessStatusCode();

        var artistMsg = await artist.PostAsJsonAsync(
            $"/api/threads/{threadId}/messages",
            new SendMessageRequest("Just yourself. See you at 3 PM."));
        artistMsg.EnsureSuccessStatusCode();

        var threadMessages = (await (await customer.GetAsync($"/api/threads/{threadId}/messages"))
            .Content.ReadFromJsonAsync<MessagePageResponse>())!;
        threadMessages.Items.Should().HaveCount(2);

        // 6. Artist marks the booking InProgress, then Completed.
        (await artist.PostAsync($"/api/bookings/{bookingId}/in-progress", null))
            .EnsureSuccessStatusCode();
        (await artist.PostAsync($"/api/bookings/{bookingId}/complete", null))
            .EnsureSuccessStatusCode();

        var completed = await GetBookingDetailAsync(customer, bookingId);
        completed.Status.Should().Be("Completed");
        completed.CompletedAt.Should().NotBeNull();

        // 7. Customer submits private feedback.
        var feedbackResp = await customer.PostAsJsonAsync(
            $"/api/bookings/{bookingId}/feedback",
            new SubmitBookingFeedbackRequest(5, 5, 5, true, "Great session."));
        feedbackResp.EnsureSuccessStatusCode();

        // 8. Sanity: a notification was dispatched at every key transition (request, accept,
        // new message). We don't assert exact count — phases overlap and the webhook fires
        // a thread-open without an additional message. Just that the customer + artist each
        // received at least one email.
        var customerEmail = await GetUserEmailAsync(_fixture, customerAuth.UserId);
        var artistEmail = await GetUserEmailAsync(_fixture, artistAuth.UserId);
        _fixture.Emails.Sent.Should().Contain(s => s.To == customerEmail);
        _fixture.Emails.Sent.Should().Contain(s => s.To == artistEmail);

        // 9. Stripe side-effects fired exactly the way we expect.
        _fixture.FakeStripe.CapturedIntents.Should().Contain(paymentIntentId);
    }

    // --- helpers ---

    private static async Task<Guid> CreateStudioAsync(HttpClient artistClient)
    {
        var resp = await artistClient.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Happy Path Ave",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task SetWeeklyOpenPatternAsync(HttpClient artistClient)
    {
        var weekly = new SetAvailabilityPatternRequest(new[]
        {
            new AvailabilityPatternDayRequest("Monday",    "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Tuesday",   "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Wednesday", "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Thursday",  "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Friday",    "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Saturday",  "Available", 8m, null, null),
            new AvailabilityPatternDayRequest("Sunday",    "Closed",    null, null, null),
        });
        (await artistClient.PutAsJsonAsync("/api/availability/pattern", weekly)).EnsureSuccessStatusCode();
    }

    private static async Task<BookingDetailResponse> GetBookingDetailAsync(HttpClient client, Guid bookingId)
    {
        var resp = await client.GetAsync($"/api/bookings/{bookingId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<BookingDetailResponse>())!;
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task MarkPaymentActiveAsync(WebAppFixture fixture, Guid artistId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.StripeConnectAccountId ??= $"acct_test_{artist.Id:N}";
        artist.PaymentStatus = ArtistPaymentStatus.Active;
        await db.SaveChangesAsync();
    }

    private static async Task<string> GetStripeIntentIdAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        booking.StripePaymentIntentId.Should().NotBeNullOrEmpty();
        return booking.StripePaymentIntentId!;
    }

    private static async Task<Guid> GetThreadIdAsync(WebAppFixture fixture, Guid bookingId)
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
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.Email!).FirstAsync();
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

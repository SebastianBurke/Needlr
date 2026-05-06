using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Studios;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Stripe;

public class StripeEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public StripeEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    // ---- Connect onboarding ----

    [Fact]
    public async Task ConnectAccount_FirstCall_CreatesAccount_SetsOnboardingInProgress()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        // Phase 11 doesn't ship a controller for Connect onboarding (FE/admin tooling lands
        // in Phases 20+); we exercise it via direct mediator dispatch here.
        var connectAccountId = await SendCreateConnectAccountAsync(_fixture, auth.UserId);
        connectAccountId.Should().StartWith("acct_test_");

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.StripeConnectAccountId.Should().Be(connectAccountId);
        artist.PaymentStatus.Should().Be(ArtistPaymentStatus.OnboardingInProgress);
    }

    [Fact]
    public async Task ConnectAccount_SecondCall_IsIdempotent()
    {
        var (_, auth) = await AuthHelpers.CreateArtistClient(_fixture);

        var first = await SendCreateConnectAccountAsync(_fixture, auth.UserId);
        var second = await SendCreateConnectAccountAsync(_fixture, auth.UserId);
        first.Should().Be(second);
    }

    [Fact]
    public async Task OnboardingLink_BeforeConnectAccount_FailsPrecondition()
    {
        var (_, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var result = await SendGenerateOnboardingLinkAsync(_fixture, auth.UserId);
        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(Needlr.Application.Common.Results.Error.FailedPreconditionCode);
    }

    [Fact]
    public async Task OnboardingLink_AfterConnectAccount_ReturnsHostedUrl()
    {
        var (_, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        await SendCreateConnectAccountAsync(_fixture, auth.UserId);
        var result = await SendGenerateOnboardingLinkAsync(_fixture, auth.UserId);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("https://stripe.test/onboard/");
    }

    // ---- Booking-flow Stripe wiring ----

    [Fact]
    public async Task RequestBooking_Creates_PaymentIntent_AndStoresIdOnBooking()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var resp = await customer.PostAsJsonAsync("/api/bookings", new RequestBookingRequest(
            artistId, "TattooSession", date, 2m, "Reference image attached.",
            "Forearm", "pm_card_visa"));
        resp.EnsureSuccessStatusCode();
        var bookingId = (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var detail = (await (await customer.GetAsync($"/api/bookings/{bookingId}"))
            .Content.ReadFromJsonAsync<BookingDetailResponse>())!;
        detail.Status.Should().Be("Requested");

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var booking = await db.Bookings.FirstAsync(b => b.Id == bookingId);
        booking.StripePaymentIntentId.Should().NotBeNullOrEmpty();
        booking.StripePaymentIntentId.Should().StartWith("pi_test_");
    }

    [Fact]
    public async Task AcceptBooking_Captures_Intent_OnConnectAccount()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);

        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        _fixture.FakeStripe.CapturedIntents.Should().Contain(booking.StripePaymentIntentId);
    }

    [Fact]
    public async Task DeclineBooking_Cancels_Intent()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var booking = await GetBookingDirectAsync(_fixture, bookingId);

        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/decline",
            new DeclineBookingRequest("OutsideMyStyle", null))).EnsureSuccessStatusCode();

        _fixture.FakeStripe.CancelledIntents.Should().Contain(booking.StripePaymentIntentId);
    }

    [Fact]
    public async Task CustomerCancel_PreAuthOnly_Cancels_Intent_NoRefund()
    {
        var (_, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var booking = await GetBookingDirectAsync(_fixture, bookingId);

        var resp = await customer.PostAsync($"/api/bookings/{bookingId}/cancel-customer", null);
        resp.EnsureSuccessStatusCode();
        var body = (await resp.Content.ReadFromJsonAsync<CancelBookingResponse>())!;
        body.RefundedAmountCad.Should().BeGreaterThan(0m); // Pre-confirmed → full refund per CancellationRefundPolicy

        _fixture.FakeStripe.CancelledIntents.Should().Contain(booking.StripePaymentIntentId);
        _fixture.FakeStripe.Refunds.Should().NotContain(r => r.Intent == booking.StripePaymentIntentId);
    }

    [Fact]
    public async Task ArtistCancel_PostAccept_FullRefund_HitsStripe()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        await SetCancellationPolicyAsync(_fixture, artistId, CancellationPolicy.Strict);
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var booking = await GetBookingDirectAsync(_fixture, bookingId);

        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var resp = await artist.PostAsync($"/api/bookings/{bookingId}/cancel-artist", null);
        resp.EnsureSuccessStatusCode();
        var body = (await resp.Content.ReadFromJsonAsync<CancelBookingResponse>())!;
        body.RefundedAmountCad.Should().Be(booking.DepositAmountCad);

        _fixture.FakeStripe.Refunds.Should().Contain(r =>
            r.Intent == booking.StripePaymentIntentId && r.Amount == booking.DepositAmountCad);
    }

    // ---- Webhook ----

    [Fact]
    public async Task Webhook_BadSignature_Returns400()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var content = new StringContent("{\"id\":\"evt_test\",\"type\":\"account.updated\"}",
            Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        anonymous.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=deadbeef");

        var resp = await anonymous.PostAsync("/api/webhooks/stripe", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_AccountUpdated_FlipsArtistToActive()
    {
        var (_, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        await SendCreateConnectAccountAsync(_fixture, auth.UserId);
        var connectAccountId = await GetConnectAccountIdAsync(_fixture, artistId);

        var payload = $$"""
        {
            "id":"evt_test_account_{{Guid.NewGuid():N}}",
            "type":"account.updated",
            "data": {
                "object": {
                    "id":"{{connectAccountId}}",
                    "object":"account",
                    "charges_enabled": true,
                    "details_submitted": true
                }
            }
        }
        """;
        var resp = await SendWebhookAsync(payload);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.PaymentStatus.Should().Be(ArtistPaymentStatus.Active);
    }

    [Fact]
    public async Task Webhook_PaymentIntentSucceeded_FlipsBookingToConfirmed()
    {
        var (artist, _, artistId) = await CreateArtistWithStudioAndPattern();
        var (customer, _) = await AuthHelpers.CreateCustomerClient(_fixture);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var bookingId = await CreateRequestedBookingAsync(customer, artistId, date, 2m);
        var sessionUtc = date.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
        (await artist.PostAsJsonAsync($"/api/bookings/{bookingId}/accept",
            new AcceptBookingRequest(sessionUtc))).EnsureSuccessStatusCode();

        var booking = await GetBookingDirectAsync(_fixture, bookingId);
        var payload = $$"""
        {
            "id":"evt_test_pi_{{Guid.NewGuid():N}}",
            "type":"payment_intent.succeeded",
            "data": {
                "object": {
                    "id":"{{booking.StripePaymentIntentId}}",
                    "object":"payment_intent",
                    "status":"succeeded"
                }
            }
        }
        """;
        var resp = await SendWebhookAsync(payload);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await GetBookingDirectAsync(_fixture, bookingId);
        refreshed.Status.Should().Be(BookingStatus.Confirmed);
        refreshed.DepositCapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Webhook_DuplicateEventId_IsIdempotent()
    {
        var payload = $$"""
        {
            "id":"evt_test_dup_{{Guid.NewGuid():N}}",
            "type":"payment_intent.canceled",
            "data": {
                "object": {
                    "id":"pi_unknown",
                    "object":"payment_intent"
                }
            }
        }
        """;

        var first = await SendWebhookAsync(payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await SendWebhookAsync(payload);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var rows = await db.StripeProcessedEvents.CountAsync();
        rows.Should().BeGreaterOrEqualTo(1);
    }

    // ---- helpers ----

    private async Task<HttpResponseMessage> SendWebhookAsync(string payload)
    {
        var anonymous = _fixture.Factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("Stripe-Signature",
            StripeSignatureHelper.Sign(payload, WebAppFixture.TestStripeWebhookSecret));
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await anonymous.PostAsync("/api/webhooks/stripe", content);
    }

    private async Task<(HttpClient Client, Needlr.Contracts.Auth.AuthResponse Auth, Guid ArtistId)>
        CreateArtistWithStudioAndPattern()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Stripe Test Ave",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();

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

    private static async Task<string> SendCreateConnectAccountAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var http = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        await ImpersonateAsync(scope.ServiceProvider, userId);
        var result = await mediator.Send(
            new Needlr.Application.Stripe.CreateConnectAccount.CreateConnectAccountCommand());
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private static async Task<Needlr.Application.Common.Results.Result<string>>
        SendGenerateOnboardingLinkAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        await ImpersonateAsync(scope.ServiceProvider, userId);
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        return await mediator.Send(
            new Needlr.Application.Stripe.GenerateOnboardingLink.GenerateOnboardingLinkCommand());
    }

    private static Task ImpersonateAsync(IServiceProvider sp, Guid userId)
    {
        // The handlers route through ICurrentUser, which reads HttpContext. Inject a fake
        // principal so they see this user as the caller.
        var http = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, $"{userId:N}@example.com"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, UserRole.Artist.ToString()),
        };
        ctx.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
        http.HttpContext = ctx;
        return Task.CompletedTask;
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task<string> GetConnectAccountIdAsync(WebAppFixture fixture, Guid artistId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        return artist.StripeConnectAccountId!;
    }

    private static async Task<Booking> GetBookingDirectAsync(WebAppFixture fixture, Guid bookingId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        return await db.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
    }

    private static async Task SetCancellationPolicyAsync(WebAppFixture fixture, Guid artistId, CancellationPolicy policy)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.Id == artistId);
        artist.CancellationPolicy = policy;
        await db.SaveChangesAsync();
    }
}

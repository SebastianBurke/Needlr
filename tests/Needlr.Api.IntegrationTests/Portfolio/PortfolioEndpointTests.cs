using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Portfolio;

public class PortfolioEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public PortfolioEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreatePiece_ReturnsId_AndPieceIsRetrievable()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "blackwork");

        var pieceId = await CreatePiece(client, styleId);

        var anonymous = _fixture.Factory.CreateClient();
        var get = await anonymous.GetAsync($"/api/portfolio/pieces/{pieceId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var piece = (await get.Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        piece.Id.Should().Be(pieceId);
        piece.BodyPlacement.Should().Be("Forearm");
        piece.Photos.Should().HaveCount(1);
        piece.Photos[0].PhotoType.Should().Be("Fresh");
        piece.Styles.Should().ContainSingle(s => s.Id == styleId);
    }

    [Fact]
    public async Task GetPiece_Missing_Returns404()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var get = await anonymous.GetAsync($"/api/portfolio/pieces/{Guid.NewGuid()}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArtistPortfolio_ReturnsPagedSummary()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "fineline");

        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);
        await CreatePiece(client, styleId);
        await CreatePiece(client, styleId);
        await CreatePiece(client, styleId);

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/portfolio/artists/{artistId}?pageSize=2");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await resp.Content.ReadFromJsonAsync<PagedPortfolioResponse>())!;
        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(3);
        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetStudioCollectivePortfolio_IncludesAllActiveArtistsPieces()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient);

        var styleId = await GetStyleIdBySlug(_fixture, "blackwork");
        await CreatePiece(founderClient, styleId);

        // Add a second artist to the studio's roster.
        var (joinerClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        await JoinStudioAsActiveMember(_fixture, founderClient, joinerClient, studioId);
        await CreatePiece(joinerClient, styleId);

        var anonymous = _fixture.Factory.CreateClient();
        var resp = await anonymous.GetAsync($"/api/portfolio/studios/{studioId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await resp.Content.ReadFromJsonAsync<PagedPortfolioResponse>())!;
        page.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AddSessionPhoto_ToOwnPiece_Succeeds_AppearsOnPiece()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "linework");
        var pieceId = await CreatePiece(client, styleId);

        using var multipart = BuildPhotoMultipart();
        multipart.Add(new StringContent("Fresh"), "PhotoType");
        multipart.Add(new StringContent("1"), "Order");
        var resp = await client.PostAsync($"/api/portfolio/pieces/{pieceId}/photos", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _fixture.Factory.CreateClient().GetAsync($"/api/portfolio/pieces/{pieceId}");
        var piece = (await get.Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        piece.Photos.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddSessionPhoto_ToOtherArtistsPiece_Returns403()
    {
        var (ownerClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "dotwork");
        var pieceId = await CreatePiece(ownerClient, styleId);

        var (otherClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        using var multipart = BuildPhotoMultipart();
        multipart.Add(new StringContent("Fresh"), "PhotoType");
        multipart.Add(new StringContent("1"), "Order");
        var resp = await otherClient.PostAsync($"/api/portfolio/pieces/{pieceId}/photos", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HidePhoto_ByOwner_SucceedsAndMarksHidden()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "geometric");
        var pieceId = await CreatePiece(client, styleId);

        var pieceResp = await _fixture.Factory.CreateClient().GetAsync($"/api/portfolio/pieces/{pieceId}");
        var piece = (await pieceResp.Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        var photoId = piece.Photos[0].Id;

        var resp = await client.PostAsJsonAsync(
            $"/api/portfolio/photos/{photoId}/hide",
            new HideSessionPhotoRequest("NSFW content not appropriate for portfolio."));
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var pieceAfter = (await (await _fixture.Factory.CreateClient().GetAsync($"/api/portfolio/pieces/{pieceId}"))
            .Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        pieceAfter.Photos[0].IsHidden.Should().BeTrue();
        pieceAfter.Photos[0].HiddenReason.Should().Contain("NSFW");
    }

    [Fact]
    public async Task HidePhoto_ShortReason_Returns400()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "tribal");
        var pieceId = await CreatePiece(client, styleId);

        var pieceResp = await _fixture.Factory.CreateClient().GetAsync($"/api/portfolio/pieces/{pieceId}");
        var piece = (await pieceResp.Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        var photoId = piece.Photos[0].Id;

        var resp = await client.PostAsJsonAsync(
            $"/api/portfolio/photos/{photoId}/hide",
            new HideSessionPhotoRequest("ugly"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadHealedPhoto_ByCustomer_AppendsHealedPhotoToLinkedPiece()
    {
        // Set up: create artist + customer + completed booking + portfolio piece linked to booking.
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var (customerClient, customerAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var styleId = await GetStyleIdBySlug(_fixture, "realism");
        var studioId = await CreateStudio(artistClient);

        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        var bookingId = await SeedCompletedBookingDirectly(_fixture, customerAuth.UserId, artistId, studioId);

        // Artist creates the linked portfolio piece.
        var pieceId = await CreatePiece(artistClient, styleId, linkedBookingId: bookingId);

        // Customer uploads healed photo.
        using var multipart = BuildPhotoMultipart();
        var resp = await customerClient.PostAsync(
            $"/api/portfolio/healed-photos/{bookingId}", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var pieceAfter = (await (await _fixture.Factory.CreateClient().GetAsync($"/api/portfolio/pieces/{pieceId}"))
            .Content.ReadFromJsonAsync<PortfolioPieceResponse>())!;
        pieceAfter.Photos.Should().HaveCount(2);
        pieceAfter.Photos.Should().Contain(p =>
            p.PhotoType == "Healed" && p.UploadedByRole == "Customer");
    }

    [Fact]
    public async Task UploadHealedPhoto_ForOtherCustomersBooking_Returns404()
    {
        var (artistClient, artistAuth) = await AuthHelpers.CreateArtistClient(_fixture);
        var (otherCustomer, otherAuth) = await AuthHelpers.CreateCustomerClient(_fixture);
        var (intruder, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var studioId = await CreateStudio(artistClient);
        var artistId = await ResolveArtistIdAsync(_fixture, artistAuth.UserId);
        var bookingId = await SeedCompletedBookingDirectly(_fixture, otherAuth.UserId, artistId, studioId);

        using var multipart = BuildPhotoMultipart();
        var resp = await intruder.PostAsync($"/api/portfolio/healed-photos/{bookingId}", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- helpers ---

    private static async Task<Guid> CreatePiece(
        HttpClient client, Guid styleId, Guid? linkedBookingId = null)
    {
        using var multipart = BuildPhotoMultipart();
        multipart.Add(new StringContent("Forearm"), "BodyPlacement");
        multipart.Add(new StringContent(styleId.ToString()), "StyleIds");
        multipart.Add(new StringContent("2026"), "YearCompleted");
        multipart.Add(new StringContent("SingleSession"), "ProgressionStatus");
        if (linkedBookingId is { } bid)
            multipart.Add(new StringContent(bid.ToString()), "LinkedBookingId");
        var resp = await client.PostAsync("/api/portfolio/pieces", multipart);
        resp.EnsureSuccessStatusCode();
        var created = (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;
        return created.Id;
    }

    private static MultipartFormDataContent BuildPhotoMultipart()
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-image-bytes"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "photo.jpg");
        return multipart;
    }

    private static async Task<Guid> CreateStudio(HttpClient artistClient)
    {
        var resp = await artistClient.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Rue Saint-Denis, Montréal",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task JoinStudioAsActiveMember(
        WebAppFixture fixture, HttpClient adminClient, HttpClient joinerClient, Guid studioId)
    {
        var requestResp = await joinerClient.PostAsJsonAsync(
            "/api/affiliations/join-requests", new JoinStudioRequest(studioId));
        requestResp.EnsureSuccessStatusCode();
        var pending = (await requestResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;
        var decide = await adminClient.PostAsJsonAsync(
            $"/api/affiliations/{pending.Id}/admin-decision",
            new AffiliationDecisionRequest(Accept: true));
        decide.EnsureSuccessStatusCode();
    }

    /// <summary>Resolves the Artist row's id directly from the DB using the user's id (since the
    /// /me/affiliations endpoint is empty until the artist creates or joins a studio).</summary>
    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task<Guid> GetStyleIdBySlug(WebAppFixture fixture, string slug)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var style = await db.TattooStyles.FirstAsync(s => s.Slug == slug);
        return style.Id;
    }

    private static async Task<Guid> SeedCompletedBookingDirectly(
        WebAppFixture fixture, Guid customerUserId, Guid artistId, Guid studioId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerUserId,
            artistId: artistId,
            studioId: studioId,
            bookingType: BookingType.TattooSession,
            requestedAt: now.AddDays(-90),
            requestedDate: DateOnly.FromDateTime(now.AddDays(-60)),
            estimatedDurationHours: 3m,
            description: "Healed-photo test booking.",
            bodyPlacement: BodyPlacement.Forearm,
            depositAmountCad: 100m,
            cancellationPolicySnapshot: CancellationPolicy.Standard);
        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = now.AddDays(-30);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }
}

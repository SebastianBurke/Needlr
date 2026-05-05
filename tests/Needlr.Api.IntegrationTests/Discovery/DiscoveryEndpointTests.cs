using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Discovery;
using Needlr.Contracts.Studios;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;
using Needlr.Infrastructure.Persistence;
using Xunit;

namespace Needlr.Api.IntegrationTests.Discovery;

public class DiscoveryEndpointTests : IClassFixture<WebAppFixture>
{
    private static readonly Guid MontrealJurisdictionId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Approximate downtown-Montréal centre.</summary>
    private const double MtlLat = 45.5019;
    private const double MtlLng = -73.5674;

    private readonly WebAppFixture _fixture;

    public DiscoveryEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Search_WithinBoundingBox_ReturnsVerifiedStudios()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var insideId = await CreateStudio(artistClient, lat: MtlLat, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, insideId);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01);
        var response = await anonymous.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().Contain(s => s.Id == insideId);
        page.Items.Where(s => s.Id == insideId).Single().IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Search_OutsideBoundingBox_ExcludesFarStudios()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        // Far studio in Toronto-ish coords.
        var farId = await CreateStudio(artistClient, lat: 43.6532, lng: -79.3832);
        await SeedVerifiedHealthInspection(_fixture, farId);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.05, MtlLng - 0.05, MtlLat + 0.05, MtlLng + 0.05);
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().NotContain(s => s.Id == farId);
    }

    [Fact]
    public async Task Search_VerifiedOnly_ExcludesDocumentsSubmitted()
    {
        var (a1, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var verified = await CreateStudio(a1, lat: MtlLat, lng: MtlLng);
        await SeedHealthInspection(_fixture, verified, VerificationStatus.Verified);

        var (a2, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var pending = await CreateStudio(a2, lat: MtlLat + 0.001, lng: MtlLng + 0.001);
        await SeedHealthInspection(_fixture, pending, VerificationStatus.DocumentsSubmitted);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01,
            verifiedOnly: true);
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().Contain(s => s.Id == verified);
        page.Items.Should().NotContain(s => s.Id == pending);
    }

    [Fact]
    public async Task Search_VerifiedOff_IncludesDocumentsSubmitted()
    {
        var (a1, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var pending = await CreateStudio(a1, lat: MtlLat, lng: MtlLng);
        await SeedHealthInspection(_fixture, pending, VerificationStatus.DocumentsSubmitted);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01,
            verifiedOnly: false);
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().Contain(s => s.Id == pending);
        page.Items.Where(s => s.Id == pending).Single().HasSubmittedDocuments.Should().BeTrue();
    }

    [Fact]
    public async Task Search_NoCredential_ExcludedAlways()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var noCred = await CreateStudio(artistClient, lat: MtlLat, lng: MtlLng);
        // No credential seeded.

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01,
            verifiedOnly: false);
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().NotContain(s => s.Id == noCred);
    }

    [Fact]
    public async Task Search_DistanceSort_ReturnsClosestFirst()
    {
        var (a1, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var nearId = await CreateStudio(a1, lat: MtlLat + 0.0005, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, nearId);

        var (a2, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var farId = await CreateStudio(a2, lat: MtlLat + 0.005, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, farId);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01);
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        var nearIndex = page.Items.ToList().FindIndex(s => s.Id == nearId);
        var farIndex = page.Items.ToList().FindIndex(s => s.Id == farId);
        nearIndex.Should().BeGreaterOrEqualTo(0);
        farIndex.Should().BeGreaterOrEqualTo(0);
        nearIndex.Should().BeLessThan(farIndex, "the closer studio should come first");
    }

    [Fact]
    public async Task Search_PageSize_RespectsLimit()
    {
        // Three verified studios in the box.
        var (a1, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var s1 = await CreateStudio(a1, lat: MtlLat, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, s1);
        var (a2, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var s2 = await CreateStudio(a2, lat: MtlLat + 0.0002, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, s2);
        var (a3, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var s3 = await CreateStudio(a3, lat: MtlLat + 0.0004, lng: MtlLng);
        await SeedVerifiedHealthInspection(_fixture, s3);

        var anonymous = _fixture.Factory.CreateClient();
        var url = BuildSearchUrl(MtlLat - 0.01, MtlLng - 0.01, MtlLat + 0.01, MtlLng + 0.01)
            + "&pageSize=2";
        var response = await anonymous.GetAsync(url);
        var page = (await response.Content.ReadFromJsonAsync<DiscoveryPageResponse>())!;

        page.Items.Should().HaveCount(2);
        page.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetArtist_ReturnsDetailWithComputedStatusAndStyles()
    {
        var (artistClient, auth) = await AuthHelpers.CreateArtistClient(_fixture);
        var artistId = await ResolveArtistIdAsync(_fixture, auth.UserId);

        // Attach a style to the artist via the DB so the response carries it.
        await AttachStyleToArtist(_fixture, artistId, "blackwork");

        // Without a verified studio, computed status defaults to Unverified or DocumentsSubmitted.
        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/artists/{artistId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var artist = (await response.Content.ReadFromJsonAsync<ArtistDetailResponse>())!;

        artist.Id.Should().Be(artistId);
        artist.Styles.Should().ContainSingle(s => s.Slug == "blackwork");
    }

    [Fact]
    public async Task GetArtist_Missing_Returns404()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/artists/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- helpers ---

    private static string BuildSearchUrl(
        double southLat, double westLng, double northLat, double eastLng,
        bool verifiedOnly = true,
        bool acceptingNewBookings = true)
    {
        var center = new
        {
            Lat = (southLat + northLat) / 2,
            Lng = (westLng + eastLng) / 2
        };
        return "/api/discovery/studios" +
            $"?southLat={southLat}&westLng={westLng}" +
            $"&northLat={northLat}&eastLng={eastLng}" +
            $"&centerLat={center.Lat}&centerLng={center.Lng}" +
            $"&verifiedOnly={(verifiedOnly ? "true" : "false")}" +
            $"&acceptingNewBookings={(acceptingNewBookings ? "true" : "false")}";
    }

    private static async Task<Guid> CreateStudio(HttpClient client, double lat, double lng)
    {
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(lat, lng),
            Address: "Test address",
            JoinPolicy: "Open"));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static Task SeedVerifiedHealthInspection(WebAppFixture fixture, Guid studioId) =>
        SeedHealthInspection(fixture, studioId, VerificationStatus.Verified);

    private static async Task SeedHealthInspection(
        WebAppFixture fixture, Guid studioId, VerificationStatus status)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        var cred = new StudioCredential(
            id: Guid.NewGuid(),
            studioId: studioId,
            jurisdictionId: MontrealJurisdictionId,
            credentialType: StudioCredentialType.HealthInspection,
            issuedDate: DateOnly.FromDateTime(now.AddMonths(-2)),
            expiryDate: DateOnly.FromDateTime(now.AddMonths(10)),
            documentUrl: $"test-credential/{studioId:N}");
        cred.VerificationStatus = status;
        if (status == VerificationStatus.Verified)
            cred.VerifiedAt = now;
        db.StudioCredentials.Add(cred);
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> ResolveArtistIdAsync(WebAppFixture fixture, Guid userId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.FirstAsync(a => a.UserId == userId);
        return artist.Id;
    }

    private static async Task AttachStyleToArtist(WebAppFixture fixture, Guid artistId, string slug)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        var artist = await db.Artists.Include(a => a.Styles).FirstAsync(a => a.Id == artistId);
        var style = await db.TattooStyles.FirstAsync(s => s.Slug == slug);
        artist.Styles.Add(style);
        await db.SaveChangesAsync();
    }
}

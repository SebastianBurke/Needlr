using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Studios;
using Needlr.Contracts.Verification;
using Xunit;

namespace Needlr.Api.IntegrationTests.Verification;

public class CredentialsEndpointTests : IClassFixture<WebAppFixture>
{
    /// <summary>The Montréal jurisdiction id seeded by <c>DataSeeder</c>.</summary>
    private static readonly Guid MontrealJurisdictionId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly WebAppFixture _fixture;

    public CredentialsEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ArtistUploadOwnCredential_AppearsInQueue_AdminApproves_LeavesQueue()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);

        var uploadResp = await PostUploadAsync(
            artistClient,
            "/api/credentials/artists/me",
            credentialType: "BloodbornePathogenCertification",
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1).Date),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11).Date));
        uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await uploadResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var (adminClient, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var queueResp = await adminClient.GetAsync("/api/admin/verification-queue");
        queueResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var queue = (await queueResp.Content.ReadFromJsonAsync<List<VerificationQueueItemResponse>>())!;
        queue.Should().Contain(i => i.Id == created.Id && i.Kind == "Artist");

        var review = await adminClient.PostAsJsonAsync(
            $"/api/admin/credentials/Artist/{created.Id}/review",
            new ReviewCredentialRequest(Approve: true));
        review.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var queueAfter = (await (await adminClient.GetAsync("/api/admin/verification-queue"))
            .Content.ReadFromJsonAsync<List<VerificationQueueItemResponse>>())!;
        queueAfter.Should().NotContain(i => i.Id == created.Id);
    }

    [Fact]
    public async Task StudioAdminUploadsStudioCredential_QueuedThenRejectedWithReason()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(artistClient);

        var uploadResp = await PostUploadAsync(
            artistClient,
            $"/api/credentials/studios/{studioId}",
            credentialType: "HealthInspection",
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2).Date),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10).Date));
        uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await uploadResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var (adminClient, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var review = await adminClient.PostAsJsonAsync(
            $"/api/admin/credentials/Studio/{created.Id}/review",
            new ReviewCredentialRequest(Approve: false, RejectionReason: "Document is illegible."));
        review.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task NonAdminArtist_UploadsToOtherStudio_Returns403()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient);

        var (otherClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await PostUploadAsync(
            otherClient,
            $"/api/credentials/studios/{studioId}",
            credentialType: "HealthInspection",
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2).Date),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10).Date));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_UploadingArtistCredential_Returns403()
    {
        var (customerClient, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var resp = await PostUploadAsync(
            customerClient,
            "/api/credentials/artists/me",
            credentialType: "BloodbornePathogenCertification",
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2).Date),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10).Date));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonAdmin_AccessVerificationQueue_Returns403()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await artistClient.GetAsync("/api/admin/verification-queue");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonAdmin_ReviewingCredential_Returns403()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await artistClient.PostAsJsonAsync(
            $"/api/admin/credentials/Artist/{Guid.NewGuid()}/review",
            new ReviewCredentialRequest(Approve: true));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCannotReviewMissingCredential_Returns404()
    {
        var (adminClient, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var resp = await adminClient.PostAsJsonAsync(
            $"/api/admin/credentials/Artist/{Guid.NewGuid()}/review",
            new ReviewCredentialRequest(Approve: true));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminCannotReviewAlreadyReviewedCredential_Returns412()
    {
        var (artistClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var uploadResp = await PostUploadAsync(
            artistClient, "/api/credentials/artists/me",
            credentialType: "BloodbornePathogenCertification",
            issuedDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1).Date),
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11).Date));
        var created = (await uploadResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var (adminClient, _) = await AuthHelpers.CreateAdminClient(_fixture);
        var first = await adminClient.PostAsJsonAsync(
            $"/api/admin/credentials/Artist/{created.Id}/review",
            new ReviewCredentialRequest(Approve: true));
        first.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var second = await adminClient.PostAsJsonAsync(
            $"/api/admin/credentials/Artist/{created.Id}/review",
            new ReviewCredentialRequest(Approve: true));
        second.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    private static async Task<HttpResponseMessage> PostUploadAsync(
        HttpClient client,
        string path,
        string credentialType,
        DateOnly issuedDate,
        DateOnly expiryDate)
    {
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake pdf bytes"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        multipart.Add(fileContent, "file", "credential.pdf");
        multipart.Add(new StringContent(MontrealJurisdictionId.ToString()), "JurisdictionId");
        multipart.Add(new StringContent(credentialType), "CredentialType");
        multipart.Add(new StringContent(issuedDate.ToString("yyyy-MM-dd")), "IssuedDate");
        multipart.Add(new StringContent(expiryDate.ToString("yyyy-MM-dd")), "ExpiryDate");
        return await client.PostAsync(path, multipart);
    }

    private static async Task<Guid> CreateStudio(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Rue Saint-Denis, Montréal"));
        resp.EnsureSuccessStatusCode();
        var created = (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;
        return created.Id;
    }
}

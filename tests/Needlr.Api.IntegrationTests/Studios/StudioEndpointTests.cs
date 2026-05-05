using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Studios;
using Xunit;

namespace Needlr.Api.IntegrationTests.Studios;

public class StudioEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public StudioEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateStudio_AsArtist_Succeeds_AndIsRetrievableById()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);

        var create = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: $"Black Needle {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Rue Saint-Denis, Montréal"));
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await create.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var anonymous = _fixture.Factory.CreateClient();
        var get = await anonymous.GetAsync($"/api/studios/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var studio = (await get.Content.ReadFromJsonAsync<StudioResponse>())!;
        studio.Id.Should().Be(created.Id);
        studio.StudioType.Should().Be("Shop");
        // Default JoinPolicy for Shop is InviteOnly per FEATURE_SPECS.md.
        studio.JoinPolicy.Should().Be("InviteOnly");
    }

    [Fact]
    public async Task CreateStudio_Anonymous_Returns401()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: "x", StudioType: "Solo",
            Location: new GeoPointDto(45.5, -73.5), Address: "addr"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStudio_AsCustomer_Returns403()
    {
        var (client, _) = await AuthHelpers.CreateCustomerClient(_fixture);
        var response = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: "x", StudioType: "Solo",
            Location: new GeoPointDto(45.5, -73.5), Address: "addr"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStudioInfo_AsAdmin_Succeeds()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(client);

        var update = await client.PatchAsJsonAsync($"/api/studios/{studioId}", new UpdateStudioInfoRequest(
            Name: "New Name",
            Address: "2 Rue Saint-Denis",
            Description: "Updated description.",
            JoinPolicy: "Open"));
        update.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var get = await client.GetAsync($"/api/studios/{studioId}");
        var studio = (await get.Content.ReadFromJsonAsync<StudioResponse>())!;
        studio.Name.Should().Be("New Name");
        studio.JoinPolicy.Should().Be("Open");
    }

    [Fact]
    public async Task UpdateStudioInfo_AsNonAdminArtist_Returns403()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient);

        var (otherClient, _) = await AuthHelpers.CreateArtistClient(_fixture);

        var update = await otherClient.PatchAsJsonAsync(
            $"/api/studios/{studioId}",
            new UpdateStudioInfoRequest("X", "Y", null, "Open"));
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStudioById_Missing_Returns404()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/studios/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchStudios_FindsByPartialName()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var unique = $"UniqueNeedle{Guid.NewGuid():N}";
        await CreateStudio(client, name: $"{unique} Tattoo");

        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/studios?q={unique}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = (await response.Content.ReadFromJsonAsync<List<StudioSummaryResponse>>())!;
        results.Should().ContainSingle(s => s.Name.Contains(unique));
    }

    [Fact]
    public async Task GetRoster_IncludesFoundingArtist()
    {
        var (client, auth) = await AuthHelpers.CreateArtistClient(_fixture, displayName: "Founder");
        var studioId = await CreateStudio(client);

        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync($"/api/studios/{studioId}/roster");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roster = (await response.Content.ReadFromJsonAsync<StudioRosterResponse>())!;
        roster.Entries.Should().HaveCount(1);
        roster.Entries[0].Role.Should().Be("Founder");
        roster.Entries[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task RequestJoin_OnOpenStudio_Pending_AdminApproves_RosterIncludes()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient, joinPolicy: "Open");

        var (joinerClient, joinerAuth) = await AuthHelpers.CreateArtistClient(_fixture, displayName: "Joiner");
        var requestResp = await joinerClient.PostAsJsonAsync(
            "/api/affiliations/join-requests", new JoinStudioRequest(studioId));
        requestResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pending = (await requestResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        // Founder admin approves.
        var decide = await founderClient.PostAsJsonAsync(
            $"/api/affiliations/{pending.Id}/admin-decision",
            new AffiliationDecisionRequest(Accept: true));
        decide.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var roster = (await (await _fixture.Factory.CreateClient().GetAsync(
            $"/api/studios/{studioId}/roster")).Content.ReadFromJsonAsync<StudioRosterResponse>())!;
        roster.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task RequestJoin_OnInviteOnlyStudio_FailsPrecondition()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient, joinPolicy: "InviteOnly");

        var (joinerClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var resp = await joinerClient.PostAsJsonAsync(
            "/api/affiliations/join-requests", new JoinStudioRequest(studioId));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task GuestSpot_Lifecycle_RequestThenAdminAccepts()
    {
        var (founderClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(founderClient);

        var (visitorClient, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var end = start.AddDays(7);
        var requestResp = await visitorClient.PostAsJsonAsync(
            "/api/affiliations/guest-spots",
            new GuestSpotRequest(studioId, start, end));
        requestResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pending = (await requestResp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;

        var decide = await founderClient.PostAsJsonAsync(
            $"/api/affiliations/{pending.Id}/host-decision",
            new AffiliationDecisionRequest(Accept: true));
        decide.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var mine = (await (await visitorClient.GetAsync("/api/affiliations/me"))
            .Content.ReadFromJsonAsync<List<AffiliationResponse>>())!;
        var spot = mine.Single(a => a.AffiliationType == "GuestSpot");
        spot.Status.Should().Be("Active");
        spot.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task RemoveAffiliation_AsFounder_Fails()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(client);

        var mine = (await (await client.GetAsync("/api/affiliations/me"))
            .Content.ReadFromJsonAsync<List<AffiliationResponse>>())!;
        var founderAff = mine.Single(a => a.StudioId == studioId);

        var resp = await client.DeleteAsync($"/api/affiliations/{founderAff.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task ChangeRole_OnFounderRow_Fails()
    {
        var (client, _) = await AuthHelpers.CreateArtistClient(_fixture);
        var studioId = await CreateStudio(client);

        var mine = (await (await client.GetAsync("/api/affiliations/me"))
            .Content.ReadFromJsonAsync<List<AffiliationResponse>>())!;
        var founderAff = mine.Single(a => a.StudioId == studioId);

        var resp = await client.PatchAsJsonAsync(
            $"/api/affiliations/{founderAff.Id}/role",
            new ChangeAffiliationRoleRequest("Member"));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    private static async Task<Guid> CreateStudio(
        HttpClient client, string? name = null, string? joinPolicy = null)
    {
        var resp = await client.PostAsJsonAsync("/api/studios", new CreateStudioRequest(
            Name: name ?? $"Studio {Guid.NewGuid():N}",
            StudioType: "Shop",
            Location: new GeoPointDto(45.5019, -73.5674),
            Address: "1 Rue Saint-Denis, Montréal",
            JoinPolicy: joinPolicy));
        resp.EnsureSuccessStatusCode();
        var created = (await resp.Content.ReadFromJsonAsync<CreatedIdResponse>())!;
        return created.Id;
    }
}

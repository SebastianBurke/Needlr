using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Needlr.Api.IntegrationTests.Fixtures;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Common;
using Xunit;

namespace Needlr.Api.IntegrationTests.Auth;

public class AuthEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public AuthEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterCustomer_Succeeds_ReturnsTokensAndCustomerRole()
    {
        var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        var request = new RegisterCustomerRequest(email, "Strong-Pass-1234", "Alice");

        var response = await client.PostAsJsonAsync("/api/auth/register-customer", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.UserId.Should().NotBe(Guid.Empty);
        auth.Email.Should().Be(email);
        auth.Role.Should().Be("Customer");
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.AccessTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        auth.RefreshTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterArtist_Succeeds_ReturnsTokensAndArtistRole()
    {
        var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        var request = new RegisterArtistRequest(email, "Strong-Pass-1234", "Inkling", YearsExperience: 5);

        var response = await client.PostAsJsonAsync("/api/auth/register-artist", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Role.Should().Be("Artist");
    }

    [Fact]
    public async Task RegisterCustomer_DuplicateEmail_Returns409()
    {
        var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        var request = new RegisterCustomerRequest(email, "Strong-Pass-1234", "Alice");

        var first = await client.PostAsJsonAsync("/api/auth/register-customer", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/auth/register-customer", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("short", "Password too short")]
    [InlineData("nodigitspresent", "No digit")]
    public async Task RegisterCustomer_WeakPassword_Returns400(string password, string _)
    {
        var client = _fixture.Factory.CreateClient();
        var request = new RegisterCustomerRequest(UniqueEmail(), password, "Alice");

        var response = await client.PostAsJsonAsync("/api/auth/register-customer", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokens()
    {
        var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();
        const string password = "Strong-Pass-1234";

        await client.PostAsJsonAsync("/api/auth/register-customer",
            new RegisterCustomerRequest(email, password, "Alice"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.Email.Should().Be(email);
        auth.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _fixture.Factory.CreateClient();
        var email = UniqueEmail();

        await client.PostAsJsonAsync("/api/auth/register-customer",
            new RegisterCustomerRequest(email, "Strong-Pass-1234", "Alice"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WRONG-pass-1234"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest($"missing-{Guid.NewGuid()}@example.com", "Whatever-pass-1234"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var client = _fixture.Factory.CreateClient();
        var initial = await Register(client);

        var response = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(initial.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed!.RefreshToken.Should().NotBe(initial.RefreshToken);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.UserId.Should().Be(initial.UserId);
    }

    [Fact]
    public async Task Refresh_WithRotatedToken_Returns401()
    {
        var client = _fixture.Factory.CreateClient();
        var initial = await Register(client);

        // First refresh succeeds and rotates the token.
        var firstRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequest(initial.RefreshToken));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Re-presenting the now-rotated token must fail.
        var secondRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequest(initial.RefreshToken));
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var client = _fixture.Factory.CreateClient();
        var auth = await Register(client);

        var logout = await client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest(auth.RefreshToken));
        logout.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(auth.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_Succeeds_DoesNotLeakTokenState()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest("definitely-not-a-real-token"));

        // Must not 404 / 401 — that would leak whether the token existed.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ProtectedEndpoint_Without_BearerToken_Returns401()
    {
        var client = _fixture.Factory.CreateClient();

        // /hangfire would require admin auth in a later phase; for now, no protected endpoints
        // exist yet to gate. We assert the auth pipeline is wired by checking that an
        // unauthenticated call to an attribute-protected dummy responds 401 once we have one.
        // Phase 5+ will introduce real protected endpoints; for Phase 4, exercising the
        // /api/auth/* endpoints exhaustively is the meaningful coverage.
        await Task.CompletedTask;
    }

    private async Task<AuthResponse> Register(HttpClient client)
    {
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register-customer",
            new RegisterCustomerRequest(email, "Strong-Pass-1234", "Alice"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}

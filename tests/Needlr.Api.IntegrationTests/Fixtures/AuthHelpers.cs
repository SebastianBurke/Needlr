using System.Net.Http.Headers;
using System.Net.Http.Json;
using Needlr.Contracts.Auth;

namespace Needlr.Api.IntegrationTests.Fixtures;

internal static class AuthHelpers
{
    /// <summary>
    /// Registers a fresh artist and returns a new <see cref="HttpClient"/> with the bearer
    /// token pre-attached. Each call uses a unique email so callers don't collide.
    /// </summary>
    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateArtistClient(
        WebAppFixture fixture, string displayName = "Inkling", int yearsExperience = 5)
    {
        var registerClient = fixture.Factory.CreateClient();
        var email = $"artist-{Guid.NewGuid():N}@example.com";
        var response = await registerClient.PostAsJsonAsync(
            "/api/auth/register-artist",
            new RegisterArtistRequest(email, "Strong-Pass-1234", displayName, yearsExperience));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateCustomerClient(
        WebAppFixture fixture)
    {
        var registerClient = fixture.Factory.CreateClient();
        var email = $"customer-{Guid.NewGuid():N}@example.com";
        var response = await registerClient.PostAsJsonAsync(
            "/api/auth/register-customer",
            new RegisterCustomerRequest(email, "Strong-Pass-1234", "Customer"));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }
}

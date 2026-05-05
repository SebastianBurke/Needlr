using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Application.Abstractions;
using Needlr.Contracts.Auth;
using Needlr.Domain.Enums;
using Needlr.Infrastructure.Identity;

namespace Needlr.Api.IntegrationTests.Fixtures;

internal static class AuthHelpers
{
    /// <summary>
    /// Creates a fresh admin user directly via UserManager (bypassing the public auth surface,
    /// which intentionally does not expose admin self-signup) and returns an authenticated
    /// <see cref="HttpClient"/>.
    /// </summary>
    public static async Task<(HttpClient Client, Guid UserId)> CreateAdminClient(WebAppFixture fixture)
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        const string password = "Strong-Pass-1234";

        Guid userId;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                CreatedAt = clock.UtcNow,
                Role = UserRole.Admin
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            userId = user.Id;
        }

        var loginClient = fixture.Factory.CreateClient();
        var loginResp = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResp.EnsureSuccessStatusCode();
        var auth = (await loginResp.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, userId);
    }

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

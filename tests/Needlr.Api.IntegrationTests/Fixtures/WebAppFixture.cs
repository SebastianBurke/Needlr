using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Class-scoped integration-test fixture that boots a postgis container and a
/// <see cref="WebApplicationFactory{TEntryPoint}"/>-hosted Api against it. Migrations are
/// applied during <see cref="InitializeAsync"/>; the container and host are torn down in
/// <see cref="DisposeAsync"/>. Tests should use unique data (email, etc.) to avoid
/// cross-test interference within a class.
/// </summary>
public sealed class WebAppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program>? _factory;
    private string? _imageRoot;
    public FakeStripeService FakeStripe { get; } = new();

    public WebAppFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:16-3.4")
            .WithDatabase("needlr_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("Fixture not initialized.");

    /// <summary>
    /// Shared with webhook signature helpers. Long enough to satisfy Stripe.net's HMAC.
    /// </summary>
    public const string TestStripeWebhookSecret = "whsec_test_secret_for_integration_tests_only_xxxxxxx";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _imageRoot = Path.Combine(Path.GetTempPath(), "needlr-test-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_imageRoot);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
                builder.UseSetting("Jwt:Issuer", "https://needlr.test");
                builder.UseSetting("Jwt:Audience", "https://needlr.test");
                builder.UseSetting(
                    "Jwt:SigningKey",
                    "test-only-signing-key-needs-32-bytes-minimum-here-please");
                builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
                builder.UseSetting("Jwt:RefreshTokenLifetimeDays", "30");
                builder.UseSetting("ImageStorage:Backend", "Local");
                builder.UseSetting("ImageStorage:LocalRootPath", _imageRoot);

                // Phase 11. Section presence triggers IStripeWebhookProcessor registration in
                // Infrastructure DI; the values themselves are placeholders since the test
                // overrides IStripeService with FakeStripeService below. The webhook signing
                // secret is real-shaped so EventUtility.ConstructEvent has something to
                // hash against in the webhook tests.
                builder.UseSetting("Stripe:SecretKey", "sk_test_placeholder");
                builder.UseSetting("Stripe:ConnectWebhookSigningSecret", TestStripeWebhookSecret);
                builder.UseSetting("Stripe:OnboardingReturnUrl", "https://needlr.test/onboard/return");
                builder.UseSetting("Stripe:OnboardingRefreshUrl", "https://needlr.test/onboard/refresh");

                builder.ConfigureServices(services =>
                {
                    // Replace the real Stripe + Hangfire scheduler with deterministic test doubles.
                    services.RemoveAll<IStripeService>();
                    services.AddSingleton<IStripeService>(FakeStripe);
                    services.RemoveAll<IBookingExpiryScheduler>();
                    services.AddSingleton<IBookingExpiryScheduler, NoopBookingExpiryScheduler>();
                    services.RemoveAll<IThreadLockScheduler>();
                    services.AddSingleton<IThreadLockScheduler, NoopThreadLockScheduler>();
                });
            });

        // Apply migrations to the throwaway test database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeedlrDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        if (_imageRoot is not null && Directory.Exists(_imageRoot))
        {
            try { Directory.Delete(_imageRoot, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}

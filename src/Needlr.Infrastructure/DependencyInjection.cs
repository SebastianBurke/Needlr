using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Infrastructure.Common;
using Needlr.Infrastructure.Identity;
using Needlr.Infrastructure.Persistence;
using Needlr.Infrastructure.Persistence.Repositories;
using Needlr.Infrastructure.Persistence.Seeding;
using Needlr.Infrastructure.Storage;

namespace Needlr.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Needlr Infrastructure services: the EF Core <see cref="NeedlrDbContext"/>
    /// (Postgres + NetTopologySuite, snake_case naming), ASP.NET Core Identity, JWT/refresh-token
    /// services, the system clock, and Hangfire storage (Postgres "hangfire" schema). Does NOT
    /// call <c>AddHangfireServer</c> — that happens in Phase 14 when recurring jobs are wired.
    /// </summary>
    public static IServiceCollection AddNeedlrInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. Set ConnectionStrings:Postgres in " +
                "appsettings.{Environment}.json or via the ConnectionStrings__Postgres environment variable.");

        services.AddDbContext<NeedlrDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsAssembly(typeof(NeedlrDbContext).Assembly.FullName);
            });
            options.UseSnakeCaseNamingConvention();
        });

        // Register the DbContext as the IUnitOfWork implementation so handlers and the
        // TransactionBehavior depend on the Application abstraction, not the concrete context.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NeedlrDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<NeedlrDbContext>();
        // AddDefaultTokenProviders() intentionally not called — this app's auth surface is
        // password + JWT + custom rotating refresh tokens. Identity's email-confirmation and
        // password-reset token providers aren't used (their extension lives in the
        // Microsoft.AspNetCore.App shared framework; FrameworkReference required if/when needed).

        // JWT options bound from the "Jwt" config section.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Auth services — implementations of the Application-layer abstractions.
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IContactInfoStripper, Common.ContactInfoStripper>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IStudioAuthorization, StudioAuthorization>();
        services.AddScoped<IVerificationStatusService, VerificationStatusService>();
        services.AddScoped<IArtistDiscoveryService, Persistence.Discovery.ArtistDiscoveryService>();

        // Repositories.
        services.AddScoped<IStudioRepository, StudioRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IArtistStudioAffiliationRepository, ArtistStudioAffiliationRepository>();

        services.AddScoped<IStudioCredentialRepository, StudioCredentialRepository>();
        services.AddScoped<IArtistCredentialRepository, ArtistCredentialRepository>();
        services.AddScoped<IPortfolioPieceRepository, PortfolioPieceRepository>();
        services.AddScoped<ISessionPhotoRepository, SessionPhotoRepository>();
        services.AddScoped<ITattooStyleRepository, TattooStyleRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Messaging (Phase 12).
        services.AddScoped<IMessageThreadRepository, MessageThreadRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageReportRepository, MessageReportRepository>();
        services.AddScoped<IThreadLockScheduler, Hangfire.HangfireThreadLockScheduler>();
        services.AddScoped<Hangfire.LockOverdueThreadsRecurringJob>();

        // Moderation + Trust & Safety (Phase 15).
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IUserWarningRepository, Persistence.Repositories.UserWarningRepository>();
        services.AddScoped<IBookingFeedbackRepository, Persistence.Repositories.BookingFeedbackRepository>();
        services.AddScoped<IBehavioralSignalsService, Persistence.TrustSafety.BehavioralSignalsService>();
        services.AddScoped<ITrustSafetyDashboardService, Persistence.TrustSafety.TrustSafetyDashboardService>();

        // Notifications (Phase 13).
        services.AddOptions<Notifications.NotificationsOptions>()
            .Bind(configuration.GetSection(Notifications.NotificationsOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<INotificationPreferenceRepository, Persistence.Repositories.NotificationPreferenceRepository>();
        services.AddScoped<IPushSubscriptionRepository, Persistence.Repositories.PushSubscriptionRepository>();
        services.AddScoped<IEmailSender, Notifications.ConsoleEmailSender>();
        services.AddScoped<IPushNotificationSender, Notifications.ConsolePushNotificationSender>();
        services.AddScoped<INotificationDispatcher, Notifications.NotificationDispatcher>();

        // Availability (Phase 9).
        services.AddScoped<IAvailabilityPatternRepository, Persistence.Repositories.AvailabilityPatternRepository>();
        services.AddScoped<IAvailabilityOverrideRepository, Persistence.Repositories.AvailabilityOverrideRepository>();
        services.AddScoped<IBookingWindowRepository, Persistence.Repositories.BookingWindowRepository>();
        services.AddScoped<IArtistLeadTimeRepository, Persistence.Repositories.ArtistLeadTimeRepository>();
        services.AddScoped<IArtistAvailabilityProjectionRepository, Persistence.Repositories.ArtistAvailabilityProjectionRepository>();
        services.AddScoped<IAvailabilityProjector, Persistence.Availability.AvailabilityProjector>();
        services.AddScoped<Hangfire.RebuildAllAvailabilityProjectionsRecurringJob>();
        services.AddScoped<Hangfire.ExpireDueRequestedBookingsRecurringJob>();
        services.AddScoped<IBookingExpiryScheduler, Hangfire.HangfireBookingExpiryScheduler>();

        // Phase 14 recurring jobs.
        services.AddScoped<Hangfire.NightlyBookingAttachmentPurgeJob>();
        services.AddScoped<Hangfire.NightlyCredentialExpiryScanJob>();
        services.AddScoped<Hangfire.DailyHealedPhotoPromptJob>();
        services.AddScoped<Hangfire.DailyBookingReminderJob>();

        // Stripe (Phase 11). Bound + service registered when the section is present —
        // otherwise tests / minimal local runs without a key just won't have IStripeService
        // resolvable, which is loud rather than silent.
        var stripeSection = configuration.GetSection(Stripe.StripeOptions.SectionName);
        if (stripeSection.Exists())
        {
            services.AddOptions<Stripe.StripeOptions>()
                .Bind(stripeSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IStripeService, Stripe.StripeService>();
            services.AddScoped<IStripeWebhookProcessor, Stripe.StripeWebhookProcessor>();
        }

        // Image storage — backend selected via the "ImageStorage" config section.
        services.AddOptions<ImageStorageOptions>()
            .Bind(configuration.GetSection(ImageStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var imageBackend = configuration.GetSection(ImageStorageOptions.SectionName)["Backend"]
            ?? ImageStorageBackend.Local;
        if (string.Equals(imageBackend, ImageStorageBackend.R2, StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IImageStorage, R2ImageStorage>();
        else
            services.AddScoped<IImageStorage, LocalFilesystemImageStorage>();

        // Idempotent startup seed for Jurisdiction + Admin role.
        services.AddHostedService<DataSeeder>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        return services;
    }
}

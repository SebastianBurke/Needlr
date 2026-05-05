using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure.Identity;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Needlr Infrastructure services: the EF Core <see cref="NeedlrDbContext"/>
    /// (Postgres + NetTopologySuite, snake_case naming), ASP.NET Core Identity, and Hangfire
    /// storage (Postgres "hangfire" schema). Does NOT call <c>AddHangfireServer</c> — that
    /// happens in Phase 14 when recurring jobs are wired up.
    /// </summary>
    public static IServiceCollection AddNeedlrInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

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
                options.SignIn.RequireConfirmedEmail = false;  // tightened in Phase 4 if needed
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<NeedlrDbContext>();
        // Note: AddDefaultTokenProviders() is wired in Phase 4 along with email-confirmation /
        // password-reset flows that actually consume the tokens.

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

        return services;
    }
}

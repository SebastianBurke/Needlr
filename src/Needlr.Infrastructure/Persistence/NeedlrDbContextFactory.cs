using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Needlr.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> commands when generating migrations or
/// scripts. Reads the connection string from <c>NEEDLR_DESIGN_TIME_DB</c> if set; otherwise
/// falls back to the local docker-compose default. Production never goes through this path —
/// the runtime <see cref="NeedlrDbContext"/> is configured by
/// <see cref="DependencyInjection.AddNeedlrInfrastructure"/> via DI.
/// </summary>
internal sealed class NeedlrDbContextFactory : IDesignTimeDbContextFactory<NeedlrDbContext>
{
    private const string DefaultLocalConnectionString =
        "Host=localhost;Port=5432;Database=needlr_dev;Username=needlr;Password=needlr";

    public NeedlrDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NEEDLR_DESIGN_TIME_DB")
            ?? DefaultLocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<NeedlrDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsAssembly(typeof(NeedlrDbContext).Assembly.FullName);
            })
            .UseSnakeCaseNamingConvention();

        return new NeedlrDbContext(optionsBuilder.Options);
    }
}

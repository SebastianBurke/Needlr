using Needlr.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Persistence + Identity + Hangfire storage (Hangfire server starts in Phase 14).
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Connection string 'Postgres' is not configured. Set ConnectionStrings:Postgres in " +
        "appsettings.{Environment}.json or via the ConnectionStrings__Postgres environment variable.");
builder.Services.AddNeedlrInfrastructure(connectionString);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

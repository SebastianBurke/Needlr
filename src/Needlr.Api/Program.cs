using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Needlr.Api.Auth;
using Needlr.Api.Common;
using Needlr.Api.Hangfire;
using Needlr.Application;
using Needlr.Application.Abstractions;
using Needlr.Infrastructure;
using Needlr.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNeedlrInfrastructure(builder.Configuration)
    .AddNeedlrApplication();

// JWT bearer authentication. Configuration is bound by Infrastructure into JwtOptions; we
// re-read it here for the validation parameters since AddJwtBearer needs the values eagerly.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// HttpContext-backed ICurrentUser for handlers.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Translates ValidationException → 400 ApiErrorResponse.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Hangfire server + dashboard. Gated by config so the integration test host (which
// applies migrations against a throwaway Testcontainer) doesn't spin up a worker that
// races with EF Core during shutdown. Set Hangfire:EnableServer=true in appsettings or
// per-env configuration.
var hangfireServerEnabled = builder.Configuration.GetValue<bool>("Hangfire:EnableServer");
if (hangfireServerEnabled)
{
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (hangfireServerEnabled)
{
    // Admin-only dashboard. Mounted after auth middleware so HttpContext.User is populated.
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AdminOnlyDashboardAuthorizationFilter()],
    });

    // Register the recurring schedules (per BUILD_PLAN.md § Phase 14).
    using var scope = app.Services.CreateScope();
    var jobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManagerV2>();
    HangfireRecurringJobs.RegisterAll(jobs);
}

app.Run();

// Exposes the implicit top-level Program class as public so test projects
// (architecture, integration) can reference the Api assembly via typeof(Program).
public partial class Program;

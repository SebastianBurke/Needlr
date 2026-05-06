using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
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

// Trust X-Forwarded-Proto/For from the reverse proxy (Caddy in the docker-compose deploy).
// Without this, UseHttpsRedirection treats requests as plain http and 308-redirects to a
// downstream-only URL, and the request log loses the real client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS redirect runs only when the app is the TLS terminator. Behind Caddy/nginx, TLS
// terminates upstream and the internal listener is plain HTTP, so we skip the redirect
// to avoid sending clients to http://internal-host:8080 redirects.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Serve the published Blazor WASM as a static SPA from wwwroot/. The Dockerfile copies
// the Web project's published wwwroot into the API's wwwroot before container build.
// UseBlazorFrameworkFiles must come before UseStaticFiles — it knows the WASM-specific
// extensions (.dat / .dll / .wasm / .blat / .pdb) that the default static-files MIME map
// doesn't recognize, so those files would otherwise fall through to a 404 since the SPA
// fallback excludes paths that look like files.
app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();

// Serve uploaded images from disk under /media. The local image storage backend writes
// keys under ImageStorage:LocalRootPath; the public URL is /media/{key}. R2/S3 backends
// return absolute URLs from UploadAsync and don't go through this route.
var imageRoot = Path.GetFullPath(
    builder.Configuration["ImageStorage:LocalRootPath"] ?? "wwwroot/uploads");
Directory.CreateDirectory(imageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageRoot),
    RequestPath = "/media",
    ServeUnknownFileTypes = false,
});

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

// SPA fallback: any request that wasn't matched by static files, controllers, or the
// hangfire dashboard returns the WASM index.html so client-side routing handles it.
app.MapFallbackToFile("index.html");

app.Run();

// Exposes the implicit top-level Program class as public so test projects
// (architecture, integration) can reference the Api assembly via typeof(Program).
public partial class Program;

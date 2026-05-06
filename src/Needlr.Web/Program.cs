using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Needlr.Contracts.Client;
using Needlr.Web;
using Needlr.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL. Defaults to the same origin as the WASM host (dev: blazor's static host;
// prod: behind the same domain as the API). Override by setting "Api:BaseUrl" in
// appsettings.json shipped under wwwroot/.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<IAuthTokenStore, LocalStorageAuthTokenStore>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddTransient<BearerAuthHttpHandler>();

// Two HttpClients: bare (for the auth endpoints that don't expect a token) and one with
// the bearer handler (everything else, including authenticated booking + messaging
// endpoints). The bearer handler no-ops when AuthState has no token, so we can route
// anonymous reads through the authenticated client too.
builder.Services.AddHttpClient("NeedlrAnonymous", client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient("NeedlrAuthenticated", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerAuthHttpHandler>();

builder.Services.AddScoped<INeedlrApi>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new NeedlrApiClient(factory.CreateClient("NeedlrAuthenticated"));
});

builder.Services.AddScoped<PushSubscriptionRegistrar>();
builder.Services.AddScoped<UnreadBadgeService>();

// Stripe.js publishable key (FE-only). Empty in dev → BookingRequestForm shows a
// fallback notice; production injects via wwwroot appsettings.json or environment.
builder.Services.Configure<StripeWebOptions>(builder.Configuration.GetSection(StripeWebOptions.SectionName));

// Default plain HttpClient for components that need same-origin static fetches.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

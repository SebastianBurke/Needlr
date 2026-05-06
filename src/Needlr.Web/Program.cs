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

// Single HttpClient for the API. Bearer attachment used to live in a DelegatingHandler
// (BearerAuthHttpHandler), but the published WASM build silently dropped the handler from
// the pipeline — every authenticated request went out without an Authorization header,
// so the entire signed-in surface 401'd. NeedlrApiClient now attaches the bearer
// per-request via HttpRequestMessage in its SendAsync helper. Anonymous endpoints stay
// anonymous because AuthState returns a null token when nobody is signed in.
builder.Services.AddHttpClient("Needlr", client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddScoped<INeedlrApi>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var auth = sp.GetRequiredService<AuthState>();
    return new NeedlrApiClient(factory.CreateClient("Needlr"), auth);
});

builder.Services.AddScoped<PushSubscriptionRegistrar>();
builder.Services.AddScoped<UnreadBadgeService>();

// Stripe.js publishable key (FE-only). Empty in dev → BookingRequestForm shows a
// fallback notice; production injects via wwwroot appsettings.json or environment.
builder.Services.Configure<StripeWebOptions>(builder.Configuration.GetSection(StripeWebOptions.SectionName));

// Default plain HttpClient for components that need same-origin static fetches.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

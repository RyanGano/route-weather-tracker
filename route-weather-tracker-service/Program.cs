using Azure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using route_weather_tracker_service.Services;

var builder = WebApplication.CreateBuilder(args);

// ----- Azure Key Vault -----
// URI is read from config so it can be overridden per environment without
// touching code. DefaultAzureCredential resolves via:
//   - Local dev: `az login` or Visual Studio credentials + User Secrets/env var for the URI
//   - Production: Managed Identity on the Container App + KeyVaultUri env var
// The Managed Identity must hold the "Key Vault Secrets User" role on the vault.
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}
else
{
    // In development, allow running without Key Vault configured so local
    // frontend/backends can be exercised without requiring Azure auth. In
    // non-development environments, fail fast to avoid misconfiguration.
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "KeyVaultUri is not configured. Set it via User Secrets (local) or as an environment variable (Azure)."
        );
    }
}

// ----- Aspire service defaults (OpenTelemetry, health checks, service discovery) -----
builder.AddServiceDefaults();

// ----- Memory cache (5-min TTL used by PassAggregatorService) -----
builder.Services.AddMemoryCache();

// ----- HTTP clients for external APIs -----
builder.Services.AddTransient<SensitiveUrlRedactionHandler>();
builder.Services.AddHttpClient<IWsdotService, WsdotService>()
    .AddHttpMessageHandler<SensitiveUrlRedactionHandler>();
builder.Services.AddScoped<IIdahoTransportService, IdahoTransportService>();
// OpenWeather removed as a fallback; NWS is used as the sole weather source.
builder.Services.AddHttpClient<INwsService, NwsService>(client =>
{
    // NWS API requires a descriptive User-Agent and a contact From header.
    var contact = builder.Configuration["NwsContact"] ?? "dev@local";
    client.DefaultRequestHeaders.UserAgent.ParseAdd($"RouteWeatherTracker/0.1 (+{contact})");
    if (!client.DefaultRequestHeaders.Contains("From"))
        client.DefaultRequestHeaders.Add("From", contact);
})
    .AddHttpMessageHandler<SensitiveUrlRedactionHandler>();
builder.Services.AddScoped<IPassDataSource, WsdotPassDataSource>();
builder.Services.AddScoped<IPassDataSource, IdahoPassDataSource>();
builder.Services.AddScoped<IPassDataSource, MontanaPassDataSource>();
builder.Services.AddScoped<IPassDataSource, OregonPassDataSource>();
builder.Services.AddScoped<IPassDataSource, ColoradoPassDataSource>();
builder.Services.AddScoped<IPassDataSource, CaliforniaPassDataSource>();
builder.Services.AddScoped<IPassDataSource, WyomingPassDataSource>();
builder.Services.AddScoped<IPassDataSource, UtahPassDataSource>();
builder.Services.AddScoped<IPassDataSource, NevadaPassDataSource>();
builder.Services.AddScoped<IPassDataSource, NewMexicoPassDataSource>();
builder.Services.AddScoped<IPassDataSource, VirginiaPassDataSource>();
builder.Services.AddScoped<IPassDataSource, NcTnPassDataSource>();
builder.Services.AddScoped<IPassAggregatorService, PassAggregatorService>();

// HTTP client used by CamerasController to proxy HTTP-only camera snapshots.
builder.Services.AddHttpClient("camera-proxy");

// ----- Routing services (OpenRouteService + geometric pass matching) -----
builder.Services.AddSingleton<IPassLocatorService, PassLocatorService>();
// OpenRouteService requires an Authorization header with the API key.
// The global standard resilience handler has a 10-second per-attempt timeout
// which is too short for some ORS responses. Remove it and use a plain
// 30-second HttpClient timeout instead — ORS is reliable enough not to need
// automatic retries, and retrying on timeouts just multiplies wait time.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental but stable enough for this use
var orsApiKey = builder.Configuration["OpenRouteServiceApiKey"] ?? string.Empty;
builder.Services.AddHttpClient<IRoutingService, OpenRouteServiceRoutingService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    // TryAddWithoutValidation bypasses .NET header format validation;
    // ORS accepts the bare JWT token string as the Authorization value.
    c.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", orsApiKey);
})
.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

// ----- Controllers and OpenAPI -----
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ----- CORS -----
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// App Service reverse-proxies to the app over HTTP internally and sets X-Forwarded-Proto.
// Without this, UseHttpsRedirection() sees HTTP and issues an infinite redirect loop.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });
app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();

using Azure.Identity;
using route_weather_tracker_service.Services;

var builder = WebApplication.CreateBuilder(args);

// ----- Azure Key Vault -----
// URI is read from config so it can be overridden per environment without
// touching code. DefaultAzureCredential resolves via:
//   - Local dev: `az login` or Visual Studio credentials + User Secrets/env var for the URI
//   - Production: Managed Identity on the Container App + KeyVaultUri env var
// The Managed Identity must hold the "Key Vault Secrets User" role on the vault.
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (string.IsNullOrWhiteSpace(keyVaultUri))
    throw new InvalidOperationException(
        "KeyVaultUri is not configured. Set it via User Secrets (local) or as an environment variable (Azure).");
builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

// ----- Aspire service defaults (OpenTelemetry, health checks, service discovery) -----
builder.AddServiceDefaults();

// ----- Memory cache (5-min TTL used by PassAggregatorService) -----
builder.Services.AddMemoryCache();

// ----- HTTP clients for external APIs -----
builder.Services.AddTransient<SensitiveUrlRedactionHandler>();
builder.Services.AddHttpClient<IWsdotService, WsdotService>()
    .AddHttpMessageHandler<SensitiveUrlRedactionHandler>();
builder.Services.AddScoped<IIdahoTransportService, IdahoTransportService>();
builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>()
    .AddHttpMessageHandler<SensitiveUrlRedactionHandler>();
builder.Services.AddScoped<IPassDataSource, WsdotPassDataSource>();
builder.Services.AddScoped<IPassDataSource, IdahoPassDataSource>();
builder.Services.AddScoped<IPassAggregatorService, PassAggregatorService>();

// ----- Routing services (OSRM + geometric pass matching) -----
builder.Services.AddSingleton<IPassLocatorService, PassLocatorService>();
builder.Services.AddHttpClient<IRoutingService, OsrmRoutingService>();

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

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();

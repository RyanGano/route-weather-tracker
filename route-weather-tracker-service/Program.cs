using Azure.Identity;
using route_weather_tracker_service.Services;

var builder = WebApplication.CreateBuilder(args);

// ----- Azure Key Vault (optional) -----
// When KeyVaultUri is set (production / staging), secrets are pulled from Key Vault
// via DefaultAzureCredential (Managed Identity on Container Apps, az login locally).
// When it is absent or empty (local Aspire dev), secrets are expected via User Secrets
// or environment variables instead — Key Vault registration is skipped entirely.
// The Managed Identity must hold the "Key Vault Secrets User" role on the vault.
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
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
builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>()
    .AddHttpMessageHandler<SensitiveUrlRedactionHandler>();
builder.Services.AddScoped<IPassDataSource, WsdotPassDataSource>();
builder.Services.AddScoped<IPassDataSource, IdahoPassDataSource>();
builder.Services.AddScoped<IPassAggregatorService, PassAggregatorService>();

// ----- Controllers and OpenAPI -----
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ----- CORS -----
// AllowedOrigins is populated at runtime:
//   - Dev: empty → falls back to AllowAnyOrigin (Aspire vite proxy covers this)
//   - Production: Aspire AppHost injects the frontend Container App FQDN via
//     the AllowedOrigins__0 environment variable.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            throw new InvalidOperationException(
                "AllowedOrigins must be configured in non-development environments. " +
                "Set the AllowedOrigins__0 environment variable to the frontend URL.");
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

using Azure.Identity;
using route_weather_tracker_service.Services;

var builder = WebApplication.CreateBuilder(args);

// ----- Azure Key Vault -----
// DefaultAzureCredential resolves via:
//   - Local dev: `az login` or Visual Studio credentials
//   - Production: Managed Identity
// Requires "Key Vault Secrets User" role on route-weather-tracker-kv.
var keyVaultUri = new Uri("https://route-weather-tracker-kv.vault.azure.net/");
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());

// ----- Aspire service defaults (OpenTelemetry, health checks, service discovery) -----
builder.AddServiceDefaults();

// ----- Memory cache (5-min TTL used by PassAggregatorService) -----
builder.Services.AddMemoryCache();

// ----- HTTP clients for external APIs -----
builder.Services.AddHttpClient<IWsdotService, WsdotService>();
builder.Services.AddScoped<IIdahoTransportService, IdahoTransportService>();
builder.Services.AddHttpClient<IOpenWeatherService, OpenWeatherService>();
builder.Services.AddScoped<IPassAggregatorService, PassAggregatorService>();

// ----- Controllers and OpenAPI -----
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ----- CORS: allow Aspire-launched Vite frontend -----
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.AllowAnyOrigin()  // Aspire injects the origin; tighten for production
              .AllowAnyHeader()
              .AllowAnyMethod());
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

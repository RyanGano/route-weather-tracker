var builder = DistributedApplication.CreateBuilder(args);

// KeyVaultUri is read here so azd bakes it into the Container App definition
// at provision time — preventing it from being wiped on every azd provision.
// Set it via:
//   - Local: dotnet user-secrets set "KeyVaultUri" "https://..." --project route-weather-tracker-service.AppHost
//   - Production: azd env set KeyVaultUri https://...
var keyVaultUri = builder.Configuration["KeyVaultUri"]
    ?? throw new InvalidOperationException(
        "KeyVaultUri must be configured. Run: azd env set KeyVaultUri https://<vault>.vault.azure.net/");

var api = builder.AddProject<Projects.route_weather_tracker_service>("api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("KeyVaultUri", keyVaultUri);

builder.AddViteApp("frontend", "../route-weather-tracker-app")
       .WithYarn()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

builder.Build().Run();

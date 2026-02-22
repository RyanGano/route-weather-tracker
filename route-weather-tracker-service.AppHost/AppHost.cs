var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.route_weather_tracker_service>("api")
    .WithExternalHttpEndpoints();

builder.AddViteApp("frontend", "../route-weather-tracker-app")
       .WithYarn()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

// CORS: AllowedOrigins__0 is set by the post-deploy step in azure-dev.yml
// after both Container Apps are running and the frontend FQDN is known.
// Locally, AllowedOrigins is empty so the Development fallback applies.

builder.Build().Run();

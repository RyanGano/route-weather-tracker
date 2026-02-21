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

// NOTE: CORS is locked to the frontend's Container App URL via the
// AllowedOrigins__0 environment variable, which must be set manually on
// the API Container App after the first deployment (see README / deploy notes).
// In dev, AllowedOrigins is empty so CORS falls back to AllowAnyOrigin.

builder.Build().Run();

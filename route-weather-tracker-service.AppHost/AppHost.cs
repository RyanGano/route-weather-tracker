var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.route_weather_tracker_service>("api")
    .WithExternalHttpEndpoints();

var frontend = builder.AddViteApp("frontend", "../route-weather-tracker-app")
       .WithYarn()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

// Inject the frontend's public HTTPS URL into the API as AllowedOrigins__0.
// Aspire resolves this to the Container App FQDN at deploy time, so no manual
// post-deploy step is needed to configure CORS.
api.WithEnvironment("AllowedOrigins__0", frontend.GetEndpoint("https"));

builder.Build().Run();

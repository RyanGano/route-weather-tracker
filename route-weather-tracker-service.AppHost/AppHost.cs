var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.route_weather_tracker_service>("api")
    .WithExternalHttpEndpoints();

builder.AddViteApp("frontend", "../route-weather-tracker-app")
       .WithYarn()
       .WithReference(api)
       .WaitFor(api)
       .WithEnvironment("BROWSER", "none")
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints()
       .PublishAsDockerFile();

builder.Build().Run();

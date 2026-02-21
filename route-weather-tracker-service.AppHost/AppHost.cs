var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.route_weather_tracker_service>("api");

builder.AddYarnApp("frontend", "../route-weather-tracker-app", "dev")
       .WithReference(api)
       .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
       .WithHttpEndpoint(env: "PORT")
       .PublishAsDockerFile();

builder.Build().Run();

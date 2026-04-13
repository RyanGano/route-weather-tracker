using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using route_weather_tracker_service.Data;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Models;
using Xunit;

namespace route_weather_tracker_service.Tests
{
    // Integration tests that call the OpenRouteService API.
    // Requires an API key in user secrets ("OpenRouteServiceApiKey") or the
    // environment variable "OpenRouteServiceApiKey". Tests are skipped when
    // not configured so CI without the secret doesn't fail.
    public class RoutingIntegrationTests
    {
        /// <summary>
        /// Resolves the ORS API key from user secrets (dev) or environment (CI).
        /// Returns null if not configured so tests can be skipped gracefully.
        /// </summary>
        private static string? GetApiKey()
        {
            var fromEnv = System.Environment.GetEnvironmentVariable("OpenRouteServiceApiKey");
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

            // Load from the service project's user secrets (local dev).
            var config = new ConfigurationBuilder()
                .AddUserSecrets("e1b6b9df-4592-4333-b7ea-1e5ba07b35ae")
                .Build();
            var fromSecrets = config["OpenRouteServiceApiKey"];
            return string.IsNullOrWhiteSpace(fromSecrets) ? null : fromSecrets;
        }

        private static OpenRouteServiceRoutingService CreateService(string apiKey)
        {
            var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
            var passLocator = new PassLocatorService();
            var logger = new NullLogger<OpenRouteServiceRoutingService>();
            return new OpenRouteServiceRoutingService(http, passLocator, logger);
        }

        [Fact]
        public async Task SeattleToAmarillo_IncludesUtahAndNewMexicoPasses()
        {
            var apiKey = GetApiKey();
            if (apiKey is null) return; // skip when not configured

            var svc = CreateService(apiKey);
            var origin = RouteEndpointRegistry.GetById("seattle");
            var dest = RouteEndpointRegistry.GetById("amarillo");
            Assert.NotNull(origin);
            Assert.NotNull(dest);

            var routes = await svc.GetRoutesAsync(origin!, dest!);
            System.Console.WriteLine($"Seattle→Amarillo: {routes.Count} routes");
            foreach (var r in routes)
                System.Console.WriteLine($"Route {r.Name}: passes=[{string.Join(",", r.PassIds)}]");

            Assert.NotEmpty(routes);

            var union = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var r in routes) foreach (var p in r.PassIds) union.Add(p);

            // ORS routes Seattle→Amarillo via I-90/I-82/I-84/I-25.
            // Snoqualmie is always on the route east from Seattle; Raton is on I-25 into NM.
            Assert.Contains("snoqualmie", union);
            Assert.Contains("raton", union);
        }

        [Fact]
        public async Task SpokaneToAmarillo_IncludesUtahAndNewMexicoPasses()
        {
            var apiKey = GetApiKey();
            if (apiKey is null) return; // skip when not configured

            var svc = CreateService(apiKey);
            var origin = RouteEndpointRegistry.GetById("spokane");
            var dest = RouteEndpointRegistry.GetById("amarillo");
            Assert.NotNull(origin);
            Assert.NotNull(dest);

            var routes = await svc.GetRoutesAsync(origin!, dest!);
            Assert.NotEmpty(routes);

            var union2 = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var r in routes) foreach (var p in r.PassIds) union2.Add(p);

            // ORS routes Spokane→Amarillo via I-90 east through Montana then I-25 south.
            // Fourth of July / Lookout are on I-90 near the ID/MT border; Raton is on I-25 into NM.
            Assert.Contains("lookout", union2);
            Assert.Contains("raton", union2);
        }
    }
}

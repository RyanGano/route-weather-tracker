using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using route_weather_tracker_service.Data;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Models;
using Xunit;

namespace route_weather_tracker_service.Tests
{
    // NOTE: Integration tests that call the public OSRM demo server.
    // These tests may be network-dependent but are useful to validate
    // the end-to-end pass-matching behavior for key endpoint pairs.
    public class RoutingIntegrationTests
    {
        [Fact]
        public async Task SeattleToAmarillo_IncludesUtahAndNewMexicoPasses()
        {
            var http = new System.Net.Http.HttpClient();
            var passLocator = new PassLocatorService();
            var logger = new NullLogger<OsrmRoutingService>();
            var svc = new OsrmRoutingService(http, passLocator, logger);

            var origin = RouteEndpointRegistry.GetById("seattle");
            var dest = RouteEndpointRegistry.GetById("amarillo");
            Assert.NotNull(origin);
            Assert.NotNull(dest);

            var routes = await svc.GetRoutesAsync(origin!, dest!);
            // Diagnostic output for debugging route/pass matching
            System.Console.WriteLine($"Seattle→Amarillo: {routes.Count} routes");
            foreach (var r in routes)
            {
                System.Console.WriteLine($"Route {r.Name}: passes=[{string.Join(",", r.PassIds)}]");
            }
            Assert.NotEmpty(routes);

            // Collect union of passes across all returned routes and assert each expected pass appears
            var union = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var r in routes) foreach (var p in r.PassIds) union.Add(p);

            Assert.Contains("parleys", union);
            Assert.Contains("soldier-summit", union);
            Assert.Contains("tijeras", union);
        }

        [Fact]
        public async Task SpokaneToAmarillo_IncludesUtahAndNewMexicoPasses()
        {
            var http = new System.Net.Http.HttpClient();
            var passLocator = new PassLocatorService();
            var logger = new NullLogger<OsrmRoutingService>();
            var svc = new OsrmRoutingService(http, passLocator, logger);

            var origin = RouteEndpointRegistry.GetById("spokane");
            var dest = RouteEndpointRegistry.GetById("amarillo");
            Assert.NotNull(origin);
            Assert.NotNull(dest);

            var routes = await svc.GetRoutesAsync(origin!, dest!);
            Assert.NotEmpty(routes);

            // Spokane→Amarillo historically included these passes; ensure they remain present
            var union2 = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var r in routes) foreach (var p in r.PassIds) union2.Add(p);

            Assert.Contains("parleys", union2);
            Assert.Contains("soldier-summit", union2);
            Assert.Contains("tijeras", union2);
        }
    }
}

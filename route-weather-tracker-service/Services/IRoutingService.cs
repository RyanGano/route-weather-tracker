// Alias avoids ambiguity with Microsoft.AspNetCore.Routing.RouteEndpoint (pulled
// in via global usings for ASP.NET Core web apps).
using RouteEndpoint = route_weather_tracker_service.Models.RouteEndpoint;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Computes city-to-city driving routes, including alternate routes.
/// The primary implementation delegates to the OSRM public routing engine.
/// </summary>
public interface IRoutingService
{
  /// <summary>
  /// Returns up to three route options between <paramref name="origin"/> and
  /// <paramref name="destination"/>, ordered by distance.
  /// Each route includes the matched mountain pass IDs along its geometry.
  /// Returns an empty list if the routing engine responds but finds no routes.
  /// Throws <see cref="RoutingServiceUnavailableException"/> if the backend
  /// cannot be reached at all (timeouts, network errors, etc.).
  /// </summary>
  Task<List<ComputedRoute>> GetRoutesAsync(
      RouteEndpoint origin,
      RouteEndpoint destination,
      CancellationToken ct = default);
}

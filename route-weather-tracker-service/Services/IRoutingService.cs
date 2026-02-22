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
  /// <paramref name="destination"/>, ordered by OSRM recommendation score.
  /// Each route includes the matched mountain pass IDs along its geometry.
  /// Returns an empty list if OSRM is unreachable or returns no results.
  /// </summary>
  Task<List<ComputedRoute>> GetRoutesAsync(
      RouteEndpoint origin,
      RouteEndpoint destination,
      CancellationToken ct = default);
}

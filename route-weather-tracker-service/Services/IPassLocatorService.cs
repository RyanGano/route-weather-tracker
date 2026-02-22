using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Finds mountain passes from PassRegistry that lie geometrically close to a route polyline.
/// No external calls — pure in-memory computation using Haversine distance.
/// </summary>
public interface IPassLocatorService
{
  /// <summary>
  /// Returns the IDs of passes from <see cref="Data.PassRegistry"/> whose coordinates
  /// fall within <paramref name="thresholdKm"/> kilometres of any segment of
  /// <paramref name="geometry"/>. Results are ordered by their position along the route.
  /// </summary>
  IReadOnlyList<string> FindPassesOnRoute(RouteGeometry geometry, double thresholdKm = 15.0);
}

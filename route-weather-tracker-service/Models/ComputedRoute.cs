namespace route_weather_tracker_service.Models;

/// <summary>
/// A city-to-city route computed by the OSRM routing engine, with mountain passes
/// matched geometrically along the route polyline.
/// </summary>
public class ComputedRoute
{
  /// <summary>Stable identifier for this route option (e.g. "route-0", "route-1" for alternates).</summary>
  public string Id { get; init; } = string.Empty;

  /// <summary>Human-readable name derived from the primary highways used (e.g. "I-90" or "I-90 / US-2").</summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>Major highway reference numbers extracted from OSRM step data (e.g. ["I-90", "US-2"]).</summary>
  public IReadOnlyList<string> HighwaysUsed { get; init; } = [];

  /// <summary>Total route distance in miles.</summary>
  public double DistanceMiles { get; init; }

  /// <summary>OSRM estimated drive time in minutes (no traffic).</summary>
  public double EstimatedMinutes { get; init; }

  /// <summary>
  /// Pass IDs from PassRegistry that lie within the geometric proximity threshold of this route.
  /// These can be fetched in full via IPassAggregatorService.
  /// </summary>
  public IReadOnlyList<string> PassIds { get; init; } = [];

  /// <summary>
  /// GeoJSON LineString geometry of the full route. Null if the OSRM response
  /// did not include geometry (should not happen with overview=full).
  /// </summary>
  public RouteGeometry? Geometry { get; init; }
}

/// <summary>GeoJSON LineString geometry returned by OSRM.</summary>
public class RouteGeometry
{
  public string Type { get; init; } = "LineString";

  /// <summary>Array of [longitude, latitude] coordinate pairs forming the route polyline.</summary>
  public IReadOnlyList<IReadOnlyList<double>> Coordinates { get; init; } = [];
}

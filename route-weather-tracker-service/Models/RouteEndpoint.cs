using System.Text.Json.Serialization;

namespace route_weather_tracker_service.Models;

/// <summary>
/// A named city or town that can serve as a trip start or end point.
/// Ordered by longitude so the API can filter passes that fall between two endpoints.
/// </summary>
public class RouteEndpoint
{
  public required string Id { get; init; }
  public required string Name { get; init; }
  public required string State { get; init; }
  public double Latitude { get; init; }
  public double Longitude { get; init; }

  /// <summary>
  /// Endpoint IDs of intermediate cities that should be used as explicit waypoints
  /// when routing to this destination, in addition to the direct route. This
  /// surfaces corridor alternatives that OSRM would otherwise omit because a
  /// shorter path exists — e.g. Kalispell is reached faster via US-2/Sandpoint
  /// but the I-90 → US-93 corridor through Missoula crosses Fourth of July and
  /// Lookout passes and represents the common road from the west.
  /// Not exposed in the API response.
  /// </summary>
  [JsonIgnore]
  public IReadOnlyList<string> RoutingHubs { get; init; } = [];
}

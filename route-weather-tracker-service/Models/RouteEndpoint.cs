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
}

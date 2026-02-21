using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Data;

/// <summary>
/// Known cities/towns along the I-90 corridor that can serve as route endpoints.
/// Listed west-to-east by longitude so filtering is unambiguous.
/// </summary>
public static class RouteEndpointRegistry
{
  public static readonly IReadOnlyList<Models.RouteEndpoint> Endpoints =
  [
      new() { Id = "stanwood",       Name = "Stanwood",       State = "WA", Latitude = 48.239, Longitude = -122.370 },
        new() { Id = "seattle",        Name = "Seattle",        State = "WA", Latitude = 47.608, Longitude = -122.335 },
        new() { Id = "spokane",        Name = "Spokane",        State = "WA", Latitude = 47.658, Longitude = -117.426 },
        new() { Id = "coeur-d-alene",  Name = "Coeur d'Alene", State = "ID", Latitude = 47.677, Longitude = -116.780 },
        new() { Id = "kalispell",      Name = "Kalispell",      State = "MT", Latitude = 48.196, Longitude = -114.313 },
        new() { Id = "missoula",       Name = "Missoula",       State = "MT", Latitude = 46.872, Longitude = -113.994 },
    ];

  public static Models.RouteEndpoint? GetById(string id) =>
      Endpoints.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

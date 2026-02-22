using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Data;

public static class RouteRegistry
{
    public static readonly IReadOnlyList<RouteInfo> Routes =
    [
        new RouteInfo
      {
          Id = "i90",
          Name = "Interstate 90",
          Highway = "I-90"
      },
      new RouteInfo
      {
          Id = "us2",
          Name = "US Highway 2",
          Highway = "US-2"
      }
    ];
}

namespace route_weather_tracker_service.Services;

/// <summary>
/// Thrown by <see cref="OsrmRoutingService"/> when every waypoint request
/// fails to reach the OSRM backend (timeouts, network errors, etc.).
/// Callers should surface a 503 rather than an empty route list so the
/// frontend can distinguish "no routes exist" from "service is down".
/// </summary>
public sealed class RoutingServiceUnavailableException : Exception
{
  public RoutingServiceUnavailableException(string message) : base(message) { }
}

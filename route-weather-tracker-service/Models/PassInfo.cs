namespace route_weather_tracker_service.Models;

public class PassInfo
{
  public string Id { get; init; } = string.Empty;
  public string Name { get; init; } = string.Empty;
  public string Highway { get; init; } = string.Empty;
  public int ElevationFeet { get; init; }
  public double Latitude { get; init; }
  public double Longitude { get; init; }
  public string State { get; init; } = string.Empty;
  public string OfficialUrl { get; init; } = string.Empty;

  /// <summary>
  /// True when a state DOT API provides real road condition data for this pass.
  /// False means conditions are derived from OpenWeatherMap data only.
  /// The frontend can use this to render a data-quality indicator.
  /// </summary>
  public bool HasOfficialConditions { get; init; } = true;
}

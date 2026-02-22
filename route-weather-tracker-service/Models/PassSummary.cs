namespace route_weather_tracker_service.Models;

public class PassSummary
{
  public PassInfo Info { get; init; } = new();
  public PassCondition? Condition { get; init; }
  public List<CameraImage> Cameras { get; init; } = new();
  public PassWeatherForecast? Weather { get; init; }
}

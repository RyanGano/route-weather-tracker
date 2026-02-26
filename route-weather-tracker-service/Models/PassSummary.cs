namespace route_weather_tracker_service.Models;

public class PassSummary
{
  public PassInfo Info { get; init; } = new();
  public PassCondition? Condition { get; init; }
  public List<CameraImage> Cameras { get; init; } = new();
  public PassWeatherForecast? Weather { get; init; }
  // Source of the weather data: "nws" | "openweather" | null
  public string? WeatherSource { get; init; }
  // Canonical URL for the forecast page used for this pass (if available)
  public string? WeatherForecastUrl { get; set; }
}

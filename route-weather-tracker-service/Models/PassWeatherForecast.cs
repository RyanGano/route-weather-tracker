namespace route_weather_tracker_service.Models;

public class WeatherForecastDay
{
  public DateOnly Date { get; init; }
  public double HighFahrenheit { get; init; }
  public double LowFahrenheit { get; init; }
  public string Description { get; init; } = string.Empty;
  public string IconCode { get; init; } = string.Empty;
  public double PrecipitationMm { get; init; }
  public double WindSpeedMph { get; init; }
}

public class PassWeatherForecast
{
  public double CurrentTempFahrenheit { get; init; }
  public string CurrentDescription { get; init; } = string.Empty;
  public string CurrentIconCode { get; init; } = string.Empty;
  public List<WeatherForecastDay> DailyForecasts { get; init; } = new();
  // Optional canonical URL for the forecast used to generate this data
  public string? SourceUrl { get; set; }
}

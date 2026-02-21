namespace route_weather_tracker_service.Models;

public class WeatherForecastDay
{
  public DateOnly Date { get; set; }
  public double HighFahrenheit { get; set; }
  public double LowFahrenheit { get; set; }
  public string Description { get; set; } = string.Empty;
  public string IconCode { get; set; } = string.Empty;
  public double PrecipitationMm { get; set; }
  public double WindSpeedMph { get; set; }
}

public class PassWeatherForecast
{
  public string PassId { get; set; } = string.Empty;
  public double CurrentTempFahrenheit { get; set; }
  public string CurrentDescription { get; set; } = string.Empty;
  public string CurrentIconCode { get; set; } = string.Empty;
  public List<WeatherForecastDay> DailyForecasts { get; set; } = new();
}

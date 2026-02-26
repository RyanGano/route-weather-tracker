using System.Net.Http.Json;
using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Fetches forecasts from the NWS API (https://api.weather.gov).
/// Returns a PassWeatherForecast compatible with the rest of the service.
/// </summary>
public class NwsService : INwsService
{
  private readonly HttpClient _http;
  private readonly ILogger<NwsService> _logger;

  public NwsService(HttpClient http, ILogger<NwsService> logger)
  {
    _http = http;
    _logger = logger;
  }

  public async Task<PassWeatherForecast?> GetForecastAsync(string passId, double latitude, double longitude, CancellationToken ct = default)
  {
    try
    {
      var pointsUrl = $"https://api.weather.gov/points/{latitude},{longitude}";
      var pointsDoc = await _http.GetFromJsonAsync<JsonDocument>(pointsUrl, ct);
      if (pointsDoc is null) return null;

      var props = pointsDoc.RootElement.GetProperty("properties");
      var forecastUrl = props.TryGetProperty("forecast", out var f) ? f.GetString() : null;
      var hourlyUrl = props.TryGetProperty("forecastHourly", out var h) ? h.GetString() : null;

      // Prefer hourly for current conditions; 7-day (forecast) for daily summaries
      JsonDocument? hourlyDoc = null;
      JsonDocument? forecastDoc = null;

      if (!string.IsNullOrWhiteSpace(hourlyUrl))
      {
        try { hourlyDoc = await _http.GetFromJsonAsync<JsonDocument>(hourlyUrl, ct); } catch { hourlyDoc = null; }
      }
      if (!string.IsNullOrWhiteSpace(forecastUrl))
      {
        try { forecastDoc = await _http.GetFromJsonAsync<JsonDocument>(forecastUrl, ct); } catch { forecastDoc = null; }
      }

      // Build current values from hourly if available, otherwise from forecast periods
      double currentTemp = 0;
      string currentDesc = string.Empty;
      string currentIcon = string.Empty;

      if (hourlyDoc != null)
      {
        var periods = hourlyDoc.RootElement.GetProperty("properties").GetProperty("periods");
        var first = periods.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Undefined)
        {
          currentTemp = first.GetProperty("temperature").GetDouble();
          currentDesc = first.GetProperty("shortForecast").GetString() ?? string.Empty;
          currentIcon = first.GetProperty("icon").GetString() ?? string.Empty;
        }
      }

      // Build daily forecasts from forecastDoc periods (which are day/night entries)
      var daily = new List<WeatherForecastDay>();
      if (forecastDoc != null)
      {
        var periods = forecastDoc.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray().ToList();
        var groups = periods.GroupBy(p => DateTimeOffset.Parse(p.GetProperty("startTime").GetString()!).Date).Take(7);
        foreach (var group in groups)
        {
          var slots = group.ToList();
          var highs = slots.Where(s => s.TryGetProperty("temperature", out _)).Select(s => s.GetProperty("temperature").GetDouble());
          var highsVal = highs.Any() ? highs.Max() : 0;
          var lowsVal = slots.Any() ? slots.Min(s => s.GetProperty("temperature").GetDouble()) : highsVal;
          var mid = slots[slots.Count / 2];
          var desc = mid.GetProperty("shortForecast").GetString() ?? string.Empty;
          var icon = mid.GetProperty("icon").GetString() ?? string.Empty;

          daily.Add(new WeatherForecastDay
          {
            Date = DateOnly.FromDateTime(group.Key),
            HighFahrenheit = highsVal,
            LowFahrenheit = lowsVal,
            Description = desc,
            IconCode = icon,
            WindSpeedMph = slots.Where(s => s.TryGetProperty("windSpeed", out _)).Select(s => ParseWind(s.GetProperty("windSpeed").GetString() ?? "0 mph")).DefaultIfEmpty(0).Average(),
            PrecipitationMm = 0
          });
        }
      }

      // Fallbacks: if we didn't get current from hourly, try forecastDoc first item
      if (string.IsNullOrEmpty(currentDesc) && forecastDoc != null)
      {
        var first = forecastDoc.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Undefined)
        {
          currentTemp = first.GetProperty("temperature").GetDouble();
          currentDesc = first.GetProperty("shortForecast").GetString() ?? string.Empty;
          currentIcon = first.GetProperty("icon").GetString() ?? string.Empty;
        }
      }

      if (daily.Count == 0) return null;

      var result = new PassWeatherForecast
      {
        CurrentTempFahrenheit = currentTemp,
        CurrentDescription = currentDesc,
        CurrentIconCode = currentIcon,
        DailyForecasts = daily
      };

      // Prefer the 7-day forecast URL for the canonical link, otherwise hourly
      result.SourceUrl = forecastUrl ?? hourlyUrl;
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "NWS error for pass {PassId}", passId);
      return null;
    }
  }

  private static double ParseWind(string s)
  {
    // Typical format: "5 to 10 mph" or "10 mph"
    try
    {
      var parts = s.Split(' ');
      foreach (var p in parts)
      {
        if (double.TryParse(p, out var v)) return v;
      }
    }
    catch { }
    return 0;
  }
}

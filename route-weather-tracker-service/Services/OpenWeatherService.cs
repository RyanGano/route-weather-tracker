using System.Net.Http.Json;
using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Fetches 5-day weather forecasts from OpenWeatherMap for a given lat/lon.
/// API key (OpenWeatherApiKey) is resolved from Azure Key Vault via IConfiguration.
/// Documentation: https://openweathermap.org/forecast5
/// </summary>
public class OpenWeatherService : IOpenWeatherService
{
  private readonly HttpClient _http;
  private readonly string _apiKey;
  private readonly ILogger<OpenWeatherService> _logger;
  private const string BaseUrl = "https://api.openweathermap.org/data/2.5";

  public OpenWeatherService(HttpClient http, IConfiguration configuration, ILogger<OpenWeatherService> logger)
  {
    _http = http;
    _logger = logger;
    _apiKey = configuration["OpenWeatherApiKey"]
        ?? throw new InvalidOperationException("OpenWeatherApiKey secret not found. Ensure it is set in Azure Key Vault.");
  }

  public async Task<PassWeatherForecast?> GetForecastAsync(string passId, double latitude, double longitude, CancellationToken ct = default)
  {
    // Current conditions
    var currentUrl = $"{BaseUrl}/weather?lat={latitude}&lon={longitude}&units=imperial&appid={_apiKey}";
    // 5-day / 3-hour forecast
    var forecastUrl = $"{BaseUrl}/forecast?lat={latitude}&lon={longitude}&units=imperial&cnt=40&appid={_apiKey}";

    try
    {
      var currentTask = _http.GetFromJsonAsync<JsonDocument>(currentUrl, ct);
      var forecastTask = _http.GetFromJsonAsync<JsonDocument>(forecastUrl, ct);

      await Task.WhenAll(currentTask, forecastTask);

      var currentDoc = await currentTask;
      var forecastDoc = await forecastTask;

      if (currentDoc is null || forecastDoc is null) return null;

      var currentRoot = currentDoc.RootElement;
      var currentTemp = currentRoot.GetProperty("main").GetProperty("temp").GetDouble();
      var currentDesc = currentRoot.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty;
      var currentIcon = currentRoot.GetProperty("weather")[0].GetProperty("icon").GetString() ?? string.Empty;

      // Aggregate 3-hour slots into daily forecasts (group by local day)
      // OpenWeather returns epoch seconds (UTC) and provides a city.timezone offset (seconds)
      var timezoneOffsetSeconds = forecastDoc.RootElement.TryGetProperty("city", out var cityElem) &&
                                  cityElem.TryGetProperty("timezone", out var tzElem)
          ? tzElem.GetInt32()
          : 0;
      var tzOffset = TimeSpan.FromSeconds(timezoneOffsetSeconds);

      var dailyGroups = forecastDoc.RootElement
          .GetProperty("list")
          .EnumerateArray()
          .GroupBy(slot =>
          {
            var unix = slot.GetProperty("dt").GetInt64();
            var localDate = DateTimeOffset.FromUnixTimeSeconds(unix).ToOffset(tzOffset).Date;
            return localDate;
          })
          .Take(5);

      var dailyForecasts = dailyGroups.Select(group =>
      {
        var slots = group.ToList();
        var highs = slots.Select(s => s.GetProperty("main").GetProperty("temp_max").GetDouble());
        var lows = slots.Select(s => s.GetProperty("main").GetProperty("temp_min").GetDouble());
        var winds = slots.Select(s => s.GetProperty("wind").GetProperty("speed").GetDouble());
        var precips = slots.Select(s =>
                  s.TryGetProperty("rain", out var rain) && rain.TryGetProperty("3h", out var r3h)
                      ? r3h.GetDouble() : 0.0);
        var midSlot = slots[slots.Count / 2];
        return new WeatherForecastDay
        {
          Date = DateOnly.FromDateTime(group.Key),
          HighFahrenheit = highs.Max(),
          LowFahrenheit = lows.Min(),
          Description = midSlot.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty,
          IconCode = midSlot.GetProperty("weather")[0].GetProperty("icon").GetString() ?? string.Empty,
          WindSpeedMph = winds.Average(),
          PrecipitationMm = precips.Sum()
        };
      }).ToList();

      return new PassWeatherForecast
      {
        CurrentTempFahrenheit = currentTemp,
        CurrentDescription = currentDesc,
        CurrentIconCode = currentIcon,
        DailyForecasts = dailyForecasts
      };
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "HTTP error fetching OpenWeather data for pass {PassId}", passId);
      return null;
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Failed to parse OpenWeather response for pass {PassId}", passId);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error fetching OpenWeather data for pass {PassId}", passId);
      throw;
    }
  }
}

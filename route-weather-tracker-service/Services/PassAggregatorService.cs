using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using route_weather_tracker_service.Data;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Aggregates data from WSDOT, Idaho 511, and OpenWeatherMap into PassSummary objects.
/// Results are cached for 5 minutes to avoid hammering free-tier APIs.
/// </summary>
public class PassAggregatorService : IPassAggregatorService
{
  private readonly IReadOnlyList<IPassDataSource> _dataSources;
  private readonly IOpenWeatherService _weather;
  private readonly IMemoryCache _cache;
  private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
  private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

  public PassAggregatorService(
      IEnumerable<IPassDataSource> dataSources,
      IOpenWeatherService weather,
      IMemoryCache cache)
  {
    _dataSources = [.. dataSources];
    _weather = weather;
    _cache = cache;
  }

  public Task<List<PassSummary>> GetAllPassesAsync(CancellationToken ct = default) =>
      GetPassesAsync(PassRegistry.Passes.Select(p => p.Id), ct);

  public async Task<List<PassSummary>> GetPassesAsync(IEnumerable<string> passIds, CancellationToken ct = default)
  {
    var summaries = await Task.WhenAll(passIds.Select(id => GetPassAsync(id, ct)));
    return summaries.Where(s => s is not null).Select(s => s!).ToList();
  }

  public async Task<PassSummary?> GetPassAsync(string passId, CancellationToken ct = default)
  {
    var cacheKey = $"pass:{passId}";

    // Fast path: already cached
    if (_cache.TryGetValue(cacheKey, out PassSummary? cached))
      return cached;

    // Acquire a per-pass semaphore to prevent a thundering herd of concurrent
    // requests all racing to populate the cache on a simultaneous miss.
    var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
    await semaphore.WaitAsync(ct);
    try
    {
      // Double-check after acquiring: a concurrent caller may have filled the cache
      if (_cache.TryGetValue(cacheKey, out cached))
        return cached;

      var info = PassRegistry.GetById(passId);
      if (info is null) return null;

      var source = _dataSources.FirstOrDefault(s => s.SupportedPassIds.Contains(passId));
      // source may be null for passes with no registered data source (OpenWeather-only fallback).
      // We still build a summary using derived conditions from weather data.

      var conditionTask = source is not null
          ? source.GetConditionAsync(passId, ct)
          : Task.FromResult<PassCondition?>(null);
      var camerasTask = source is not null
          ? source.GetCamerasAsync(passId, ct)
          : Task.FromResult(new List<CameraImage>());
      var weatherTask = _weather.GetForecastAsync(passId, info.Latitude, info.Longitude, ct);

      await Task.WhenAll(conditionTask, camerasTask, weatherTask);

      var weather = await weatherTask;
      var condition = await conditionTask ?? DeriveCondition(passId, weather);

      var summary = new PassSummary
      {
        Info = info,
        Condition = condition,
        Cameras = camerasTask.Result,
        Weather = weather
      };

      _cache.Set(cacheKey, summary, CacheTtl);
      return summary;
    }
    finally
    {
      semaphore.Release();
    }
  }

  /// <summary>
  /// Derives a PassCondition from weather data for passes that don't have an
  /// official road condition source (i.e. Idaho/Montana passes).
  /// </summary>
  private static PassCondition? DeriveCondition(string passId, PassWeatherForecast? weather)
  {
    if (weather is null) return null;

    var roadCondition = InferRoadCondition(
        weather.CurrentDescription, weather.CurrentTempFahrenheit);

    return new PassCondition
    {
      PassId = passId,
      RoadCondition = roadCondition,
      WeatherCondition = weather.CurrentDescription,
      TemperatureFahrenheit = (int)Math.Round(weather.CurrentTempFahrenheit),
      EastboundRestriction = TravelRestriction.None,
      WestboundRestriction = TravelRestriction.None,
      LastUpdated = DateTime.UtcNow
    };
  }

  private static string InferRoadCondition(string description, double tempF)
  {
    var d = description.ToLowerInvariant();
    if (d.Contains("blizzard") || d.Contains("heavy snow"))
      return tempF < 28 ? "Icy / Snow packed" : "Heavy snow";
    if (d.Contains("snow") || d.Contains("sleet"))
      return tempF < 30 ? "Snow packed / Icy" : "Snow covered";
    if (d.Contains("freezing") || d.Contains("ice"))
      return "Icy / Freezing";
    if (d.Contains("rain") || d.Contains("drizzle") || d.Contains("shower"))
      return tempF < 32 ? "Freezing rain" : "Bare and wet";
    if (d.Contains("mist") || d.Contains("fog"))
      return "Bare and wet";
    return "Bare and dry";
  }
}

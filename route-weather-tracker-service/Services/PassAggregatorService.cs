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
    private readonly IWsdotService _wsdot;
    private readonly IIdahoTransportService _idaho;
    private readonly IOpenWeatherService _weather;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public PassAggregatorService(
        IWsdotService wsdot,
        IIdahoTransportService idaho,
        IOpenWeatherService weather,
        IMemoryCache cache)
    {
        _wsdot   = wsdot;
        _idaho   = idaho;
        _weather = weather;
        _cache   = cache;
    }

    public async Task<List<PassSummary>> GetAllPassesAsync(CancellationToken ct = default)
    {
        var summaries = await Task.WhenAll(
            PassRegistry.Passes.Select(p => GetPassAsync(p.Id, ct)));
        return summaries.Where(s => s is not null).Select(s => s!).ToList();
    }

    public async Task<PassSummary?> GetPassAsync(string passId, CancellationToken ct = default)
    {
        var cacheKey = $"pass:{passId}";
        if (_cache.TryGetValue(cacheKey, out PassSummary? cached))
            return cached;

        var info = PassRegistry.GetById(passId);
        if (info is null) return null;

        // Fetch from appropriate sources for each pass
        var conditionTask = info.State == "WA"
            ? _wsdot.GetPassConditionAsync(passId, ct)
            : Task.FromResult<PassCondition?>(null);

        var camerasTask = info.State == "WA"
            ? _wsdot.GetPassCamerasAsync(passId, ct).ContinueWith(t => (IEnumerable<CameraImage>)t.Result, ct)
            : _idaho.GetPassCamerasAsync(passId, ct).ContinueWith(t => (IEnumerable<CameraImage>)t.Result, ct);

        var weatherTask = _weather.GetForecastAsync(passId, info.Latitude, info.Longitude, ct);

        await Task.WhenAll(conditionTask, camerasTask, weatherTask);

        var summary = new PassSummary
        {
            Info      = info,
            Condition = await conditionTask,
            Cameras   = (await camerasTask).ToList(),
            Weather   = await weatherTask
        };

        _cache.Set(cacheKey, summary, CacheTtl);
        return summary;
    }
}

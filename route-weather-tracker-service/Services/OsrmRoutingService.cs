using System.Net.Http.Json;
using System.Text.Json;
// Alias avoids ambiguity with Microsoft.AspNetCore.Routing.RouteEndpoint.
using RouteEndpoint = route_weather_tracker_service.Models.RouteEndpoint;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Routes city-to-city trips via the OSRM public routing engine (OpenStreetMap data).
/// No API key required. Demo server: https://router.project-osrm.org
/// Rate limit: ~10 req/s fair-use. For high traffic, self-host OSRM.
/// </summary>
public class OsrmRoutingService : IRoutingService
{
  private const string OsrmBase = "https://router.project-osrm.org";

  private readonly HttpClient _http;
  private readonly IPassLocatorService _passLocator;
  private readonly ILogger<OsrmRoutingService> _logger;

  public OsrmRoutingService(
      HttpClient http,
      IPassLocatorService passLocator,
      ILogger<OsrmRoutingService> logger)
  {
    _http = http;
    _passLocator = passLocator;
    _logger = logger;
  }

  public async Task<List<ComputedRoute>> GetRoutesAsync(
      RouteEndpoint origin,
      RouteEndpoint destination,
      CancellationToken ct = default)
  {
    // OSRM coordinates are lon,lat (not lat,lon)
    var url = $"{OsrmBase}/route/v1/driving/" +
              $"{origin.Longitude},{origin.Latitude};" +
              $"{destination.Longitude},{destination.Latitude}" +
              "?alternatives=true&steps=true&geometries=geojson&overview=full&annotations=false";

    try
    {
      var doc = await _http.GetFromJsonAsync<JsonDocument>(url, ct);
      if (doc is null) return [];

      var root = doc.RootElement;

      if (root.TryGetProperty("code", out var code) &&
          code.GetString() != "Ok")
      {
        _logger.LogWarning("OSRM returned non-Ok code '{Code}' for {Origin} → {Dest}",
            code.GetString(), origin.Name, destination.Name);
        return [];
      }

      if (!root.TryGetProperty("routes", out var routesEl)) return [];

      var routes = new List<ComputedRoute>();
      var idx = 0;

      foreach (var routeEl in routesEl.EnumerateArray())
      {
        var highways = ExtractHighways(routeEl);
        var geometry = ExtractGeometry(routeEl);
        var passIds = geometry is not null
            ? _passLocator.FindPassesOnRoute(geometry)
            : (IReadOnlyList<string>)[];

        var distMetres = routeEl.TryGetProperty("distance", out var distEl) ? distEl.GetDouble() : 0.0;
        var durSec = routeEl.TryGetProperty("duration", out var durEl) ? durEl.GetDouble() : 0.0;

        routes.Add(new ComputedRoute
        {
          Id = $"route-{idx}",
          Name = BuildName(highways, idx),
          HighwaysUsed = highways,
          DistanceMiles = distMetres / 1609.344,
          EstimatedMinutes = durSec / 60.0,
          PassIds = passIds,
          Geometry = geometry
        });
        idx++;
      }

      _logger.LogInformation(
          "OSRM: {Count} route(s) found {Origin} → {Dest}; passes total: {Passes}",
          routes.Count, origin.Name, destination.Name,
          routes.Sum(r => r.PassIds.Count));

      return routes;
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "OSRM HTTP error for {Origin} → {Dest}", origin.Name, destination.Name);
      return [];
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "OSRM response parse error for {Origin} → {Dest}", origin.Name, destination.Name);
      return [];
    }
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  /// <summary>
  /// Collects unique highway reference tags (e.g. "I-90", "US-2") from all
  /// route steps. Minor state highways and unclassified refs are filtered out
  /// to keep the name readable.
  /// </summary>
  private static IReadOnlyList<string> ExtractHighways(JsonElement routeEl)
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (!routeEl.TryGetProperty("legs", out var legs)) return [];

    foreach (var leg in legs.EnumerateArray())
    {
      if (!leg.TryGetProperty("steps", out var steps)) continue;
      foreach (var step in steps.EnumerateArray())
      {
        if (!step.TryGetProperty("ref", out var refEl)) continue;
        var refs = refEl.GetString();
        if (string.IsNullOrWhiteSpace(refs)) continue;

        // OSRM may return semicolon-delimited values: "I-90;US-20"
        foreach (var r in refs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
          if (IsMajorHighway(r)) seen.Add(r);
        }
      }
    }

    // Sort: interstates first, then US routes, alphabetically within each group
    return seen
        .OrderBy(h => h.StartsWith("I-", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(h => h)
        .ToList();
  }

  private static bool IsMajorHighway(string r) =>
      r.StartsWith("I-", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("US-", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("US ", StringComparison.OrdinalIgnoreCase);

  private static RouteGeometry? ExtractGeometry(JsonElement routeEl)
  {
    if (!routeEl.TryGetProperty("geometry", out var geomEl)) return null;
    if (!geomEl.TryGetProperty("coordinates", out var coordsEl)) return null;

    var coords = new List<IReadOnlyList<double>>();
    foreach (var point in coordsEl.EnumerateArray())
    {
      var pair = point.EnumerateArray().Select(v => v.GetDouble()).ToList();
      if (pair.Count >= 2) coords.Add(pair);
    }

    return coords.Count >= 2 ? new RouteGeometry { Coordinates = coords } : null;
  }

  private static string BuildName(IReadOnlyList<string> highways, int idx)
  {
    if (highways.Count == 0) return idx == 0 ? "Fastest Route" : $"Alternate Route {idx}";
    return string.Join(" / ", highways.Take(3)); // cap at 3 highways for readability
  }
}

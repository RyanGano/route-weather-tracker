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
  /// Returns the major highway designations (e.g. "I-90", "US-12") that account
  /// for a significant portion of the route.
  ///
  /// Each OSRM step carries a <c>distance</c> in metres and an optional <c>ref</c>
  /// tag (semicolon-delimited when multiple designations share a segment, e.g.
  /// "I-90;US-2" near Spokane where the two highways are briefly concurrent).
  /// Accumulating distance per highway and requiring it to reach a minimum share
  /// of the total route length eliminates brief interchange noise while correctly
  /// retaining genuine multi-highway routes like "I-90 / US-20".
  /// </summary>
  /// <param name="routeEl">A single OSRM route object.</param>
  /// <param name="minFraction">
  /// Fraction of total route distance a highway must cover to be included.
  /// Default 0.05 (5%) — keeps any highway used for at least ~14 miles on a
  /// typical 280-mile Pacific-Northwest interstate run.
  /// </param>
  private static IReadOnlyList<string> ExtractHighways(JsonElement routeEl, double minFraction = 0.05)
  {
    if (!routeEl.TryGetProperty("legs", out var legs)) return [];

    // Accumulate metres per highway designation.
    var distanceByHighway = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    double totalMetres = 0;

    foreach (var leg in legs.EnumerateArray())
    {
      if (!leg.TryGetProperty("steps", out var steps)) continue;
      foreach (var step in steps.EnumerateArray())
      {
        var stepMetres = step.TryGetProperty("distance", out var distEl) ? distEl.GetDouble() : 0.0;
        totalMetres += stepMetres;

        if (!step.TryGetProperty("ref", out var refEl)) continue;
        var refs = refEl.GetString();
        if (string.IsNullOrWhiteSpace(refs)) continue;

        // OSRM may return semicolon-delimited values: "I-90;US-2" on concurrent
        // segments. Split the step distance equally among all designations —
        // if a step is only 200 m of "I-90;US-2" each gets 100 m credit,
        // which is still negligible compared to hundreds of miles of sole I-90.
        var parts = refs
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsMajorHighway)
            .ToList();

        if (parts.Count == 0) continue;
        var share = stepMetres / parts.Count;
        foreach (var r in parts)
          distanceByHighway[r] = distanceByHighway.GetValueOrDefault(r) + share;
      }
    }

    if (totalMetres <= 0) return [];

    var threshold = totalMetres * minFraction;

    // Sort: interstates first, then US routes; alphabetically within each group.
    return distanceByHighway
        .Where(kv => kv.Value >= threshold)
        .Select(kv => kv.Key)
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

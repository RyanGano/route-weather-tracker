using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using route_weather_tracker_service.Data;
using route_weather_tracker_service.Models;
// Alias avoids ambiguity with Microsoft.AspNetCore.Routing.RouteEndpoint.
using RouteEndpoint = route_weather_tracker_service.Models.RouteEndpoint;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Routes city-to-city trips via the OpenRouteService (ORS) API.
/// https://openrouteservice.org/dev/#/api-docs/v2/directions/{profile}/post
///
/// The key is read from configuration key "OpenRouteServiceApiKey" and sent as
/// the Authorization header. Register via IRoutingService in Program.cs.
/// </summary>
public class OpenRouteServiceRoutingService : IRoutingService
{
  private const string OrsBase = "https://api.openrouteservice.org";

  private readonly HttpClient _http;
  private readonly IPassLocatorService _passLocator;
  private readonly ILogger<OpenRouteServiceRoutingService> _logger;

  public OpenRouteServiceRoutingService(
      HttpClient http,
      IPassLocatorService passLocator,
      ILogger<OpenRouteServiceRoutingService> logger)
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
    // Same waypoint-set strategy as the former OSRM service:
    // always query direct, plus one request per RoutingHub to surface
    // corridors the engine would otherwise skip.
    var waypointSets = new List<IReadOnlyList<RouteEndpoint>>();
    waypointSets.Add([origin, destination]);
    foreach (var hubId in destination.RoutingHubs)
    {
      var hub = RouteEndpointRegistry.GetById(hubId);
      if (hub is not null && hub.Id != origin.Id && hub.Id != destination.Id)
        waypointSets.Add([origin, hub, destination]);
    }
    foreach (var hubId in origin.RoutingHubs)
    {
      var hub = RouteEndpointRegistry.GetById(hubId);
      if (hub is not null && hub.Id != origin.Id && hub.Id != destination.Id)
        waypointSets.Add([origin, hub, destination]);
    }

    // Fetch all waypoint sequences in parallel.
    // Returns null on connectivity failure, [] when ORS responds but no routes.
    var fetchTasks = waypointSets
        .Select(wps => FetchFromOrsAsync(wps, origin, destination, ct));
    var allResults = await Task.WhenAll(fetchTasks);

    // If every request failed to connect, the routing service is unreachable.
    if (allResults.All(r => r is null))
      throw new RoutingServiceUnavailableException(
          $"OpenRouteService routing API is unreachable for {origin.Name} \u2192 {destination.Name}.");

    var allRoutes = allResults
        .Where(r => r is not null)
        .SelectMany(r => r!)
        .ToList();

    if (allRoutes.Count == 0) return [];

    // Deduplicate by highway fingerprint + pass set.
    var seen = new HashSet<string>();
    var unique = allRoutes
        .Where(r =>
        {
          var hwKey = string.Join(",", r.HighwaysUsed.OrderBy(h => h, StringComparer.OrdinalIgnoreCase));
          var passKey = string.Join(",", r.PassIds.OrderBy(p => p));
          var key = $"{hwKey}|{passKey}";
          if (key == "|") key = r.Name;
          return seen.Add(key);
        })
        .OrderBy(r => r.DistanceMiles)
        .ToList();

    var primaryDist = unique[0].DistanceMiles;
    unique = unique
        .Select((r, i) => r with
        {
          Id = $"route-{i}",
          ExtraDistanceMiles = i == 0 ? null : Math.Round(r.DistanceMiles - primaryDist, 1)
        })
        .ToList();

    _logger.LogInformation(
        "ORS: {Count} route(s) found {Origin} → {Dest}; passes total: {Passes}",
        unique.Count, origin.Name, destination.Name,
        unique.Sum(r => r.PassIds.Count));

    return unique;
  }

  private async Task<List<ComputedRoute>?> FetchFromOrsAsync(
      IReadOnlyList<RouteEndpoint> waypoints,
      RouteEndpoint origin,
      RouteEndpoint destination,
      CancellationToken ct)
  {
    // ORS takes [lon, lat] coordinate pairs.
    var coords = waypoints
        .Select(w => new[] { w.Longitude, w.Latitude })
        .ToArray();

    var requestBody = new { coordinates = coords, geometry = true, instructions = true };
    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var url = $"{OrsBase}/v2/directions/driving-car/geojson";

    try
    {
      var response = await _http.PostAsync(url, content, ct);
      if (!response.IsSuccessStatusCode)
      {
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("ORS HTTP {Status} for {Origin} → {Dest}: {Body}",
            (int)response.StatusCode, origin.Name, destination.Name,
            body.Length > 200 ? body[..200] : body);
        return null;
      }

      var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
      if (doc is null) return [];

      var root = doc.RootElement;
      if (!root.TryGetProperty("features", out var featuresEl)) return [];

      var routes = new List<ComputedRoute>();
      var idx = 0;

      foreach (var feature in featuresEl.EnumerateArray())
      {
        var geometry = ExtractGeometry(feature);
        var passIds = geometry is not null
            ? _passLocator.FindPassesOnRoute(geometry)
            : (IReadOnlyList<string>)[];

        var props = feature.TryGetProperty("properties", out var p) ? p : default;
        double distMetres = 0, durSec = 0;
        if (props.ValueKind == JsonValueKind.Object &&
            props.TryGetProperty("summary", out var summary))
        {
          distMetres = summary.TryGetProperty("distance", out var d) ? d.GetDouble() : 0;
          durSec = summary.TryGetProperty("duration", out var dur) ? dur.GetDouble() : 0;
        }

        // Extract highway designations from ORS step names + supplement
        // from matched pass registry entries (ORS often collapses long
        // highway stretches into a single unnamed step).
        var stepHighways = ExtractHighwaysFromSteps(props);
        var passHighways = passIds
            .Select(id => PassRegistry.GetById(id)?.Highway)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!)
            .Where(IsMajorHighway)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var highways = stepHighways
            .Concat(passHighways)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(h => h.StartsWith("I-", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(h => h)
            .ToList();

        routes.Add(new ComputedRoute
        {
          Id = $"route-tmp-{idx}",
          Name = BuildName(highways, idx),
          HighwaysUsed = highways,
          DistanceMiles = distMetres / 1609.344,
          EstimatedMinutes = durSec / 60.0,
          PassIds = passIds,
          PassNames = passIds.Select(id => PassRegistry.GetById(id)?.Name ?? id).ToList(),
          Geometry = geometry
        });
        idx++;
      }

      return routes;
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "ORS HTTP error for {Origin} → {Dest}", origin.Name, destination.Name);
      return null;
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "ORS response parse error for {Origin} → {Dest}", origin.Name, destination.Name);
      return null;
    }
    catch (Exception ex)
    {
      if (ct.IsCancellationRequested) throw;
      _logger.LogWarning(ex, "ORS unexpected error for {Origin} → {Dest}", origin.Name, destination.Name);
      return null;
    }
  }

  /// <summary>
  /// Extracts the GeoJSON LineString from the ORS Feature element.
  /// ORS geometry: feature.geometry.coordinates = [[lon,lat], ...]
  /// </summary>
  private static RouteGeometry? ExtractGeometry(JsonElement feature)
  {
    if (!feature.TryGetProperty("geometry", out var geomEl)) return null;
    if (!geomEl.TryGetProperty("coordinates", out var coordsEl)) return null;

    var coords = new List<IReadOnlyList<double>>();
    foreach (var point in coordsEl.EnumerateArray())
    {
      var pair = point.EnumerateArray().Select(v => v.GetDouble()).ToList();
      if (pair.Count >= 2) coords.Add(pair);
    }
    return coords.Count >= 2 ? new RouteGeometry { Coordinates = coords } : null;
  }

  /// <summary>
  /// Parses highway designations from ORS step <c>name</c> fields.
  /// ORS uses "I 90", "I-90", "US 93", "US-2" formats; normalizes to "I-90", "US-93".
  /// Only returns interstates and US routes (≥2% of total route distance).
  /// </summary>
  private static IReadOnlyList<string> ExtractHighwaysFromSteps(JsonElement props, double minFraction = 0.02)
  {
    if (props.ValueKind != JsonValueKind.Object) return [];
    if (!props.TryGetProperty("segments", out var segsEl)) return [];

    var distanceByHighway = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    double totalMetres = 0;

    foreach (var seg in segsEl.EnumerateArray())
    {
      if (!seg.TryGetProperty("steps", out var stepsEl)) continue;
      foreach (var step in stepsEl.EnumerateArray())
      {
        var stepMetres = step.TryGetProperty("distance", out var d) ? d.GetDouble() : 0.0;
        totalMetres += stepMetres;

        if (!step.TryGetProperty("name", out var nameEl)) continue;
        var raw = nameEl.GetString();
        if (string.IsNullOrWhiteSpace(raw) || raw == "-") continue;

        // ORS names can be comma-separated: "State Route 532, WA 532"
        // Split and look for major highway designations in each part.
        foreach (var part in raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
          if (!IsMajorHighway(part)) continue;
          var normalized = NormalizeHighway(part);
          distanceByHighway[normalized] = distanceByHighway.GetValueOrDefault(normalized) + stepMetres;
        }
      }
    }

    if (totalMetres <= 0) return [];
    var threshold = totalMetres * minFraction;

    return distanceByHighway
        .Where(kv => kv.Value >= threshold)
        .Select(kv => kv.Key)
        .OrderBy(h => h.StartsWith("I-", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(h => h)
        .ToList();
  }

  private static bool IsMajorHighway(string r) =>
      r.StartsWith("I-", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("I ", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("US-", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("US ", StringComparison.OrdinalIgnoreCase);

  private static string NormalizeHighway(string r)
  {
    if (r.Length > 2 && r[1] == ' ')
      return string.Concat(r.AsSpan(0, 1), "-", r.AsSpan(2));
    if (r.StartsWith("US ", StringComparison.OrdinalIgnoreCase) && r.Length > 3)
      return string.Concat("US-", r.AsSpan(3));
    return r;
  }

  private static string BuildName(IReadOnlyList<string> highways, int idx)
  {
    if (highways.Count == 0) return idx == 0 ? "Fastest Route" : $"Alternate Route {idx}";
    return string.Join(" / ", highways.Take(3));
  }
}

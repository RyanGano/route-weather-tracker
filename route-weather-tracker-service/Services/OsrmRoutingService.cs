using System.Net.Http.Json;
using System.Text.Json;
// Alias avoids ambiguity with Microsoft.AspNetCore.Routing.RouteEndpoint.
using RouteEndpoint = route_weather_tracker_service.Models.RouteEndpoint;
using route_weather_tracker_service.Data;
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
    // Build all waypoint sequences to query:
    //   1. Always query the direct origin → destination route (OSRM's natural choice).
    //   2. For each routing hub declared by the destination, also query
    //      origin → hub → destination. This surfaces corridor alternatives that
    //      OSRM omits because a shorter path exists — e.g. for Kalispell, OSRM
    //      picks US-2/Sandpoint (~60 mi shorter) but the I-90 → US-93 route via
    //      St. Regis is the common road and crosses Fourth of July and Lookout passes.
    //   3. Symmetrically, for each routing hub declared by the origin, also query
    //      origin → hub → destination so the same corridor is surfaced when the
    //      hub-declared city is the starting point (westbound trips).
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
    var fetchTasks = waypointSets
        .Select(wps => FetchFromOsrmAsync(wps, origin, destination, ct));
    var allRouteLists = await Task.WhenAll(fetchTasks);
    var allRoutes = allRouteLists.SelectMany(r => r).ToList();

    if (allRoutes.Count == 0) return [];

    // Deduplicate: two routes are considered identical when they cover the same
    // major highways AND the same passes. This collapses near-identical OSRM
    // geometries for the same corridor while keeping genuinely distinct options
    // (e.g. "I-90/US-93 with Idaho passes" vs "US-2/US-95 via Sandpoint").
    var seen = new HashSet<string>();
    var unique = allRoutes
        .Where(r =>
        {
          var hwKey = string.Join(",", r.HighwaysUsed.OrderBy(h => h, StringComparer.OrdinalIgnoreCase));
          var passKey = string.Join(",", r.PassIds.OrderBy(p => p));
          var key = $"{hwKey}|{passKey}";
          if (key == "|") key = r.Name; // fallback for unnamed, pass-less routes
          return seen.Add(key);
        })
        .OrderBy(r => r.DistanceMiles)
        .ToList();

    // Re-assign stable IDs and tag extra-distance relative to the fastest option.
    var primaryDist = unique[0].DistanceMiles;
    unique = unique
        .Select((r, i) => r with
        {
          Id = $"route-{i}",
          ExtraDistanceMiles = i == 0 ? null : Math.Round(r.DistanceMiles - primaryDist, 1)
        })
        .ToList();

    _logger.LogInformation(
        "OSRM: {Count} route(s) found {Origin} → {Dest}; passes total: {Passes}",
        unique.Count, origin.Name, destination.Name,
        unique.Sum(r => r.PassIds.Count));

    return unique;
  }

  /// <summary>
  /// Issues a single OSRM request for the given ordered waypoints and parses
  /// the response into <see cref="ComputedRoute"/> objects. Routes are returned
  /// with temporary IDs — the caller re-indexes after merging all results.
  /// </summary>
  private async Task<List<ComputedRoute>> FetchFromOsrmAsync(
      IReadOnlyList<RouteEndpoint> waypoints,
      RouteEndpoint origin,
      RouteEndpoint destination,
      CancellationToken ct)
  {
    var coordinateString = string.Join(";",
        waypoints.Select(w => $"{w.Longitude},{w.Latitude}"));
    var url = $"{OsrmBase}/route/v1/driving/{coordinateString}" +
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
          Id = $"route-tmp-{idx}", // re-assigned after merge
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
  /// Fraction of total route distance a highway must cover to be included in the
  /// label. Default 0.02 (2%) — eliminates genuine junction noise (a concurrent
  /// segment of &lt;0.1% of route length) while retaining connector highways that
  /// define a meaningful alternate path (e.g. US-12 over White Pass = ~6% of a
  /// Yakima→Portland trip). Pass detection is unaffected by this value — passes
  /// are located geometrically, not by highway ref.
  /// </param>
  private static IReadOnlyList<string> ExtractHighways(JsonElement routeEl, double minFraction = 0.02)
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
            .Select(NormalizeHighway)
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
      r.StartsWith("I ", StringComparison.OrdinalIgnoreCase) ||   // OSM uses "I 90" not "I-90"
      r.StartsWith("US-", StringComparison.OrdinalIgnoreCase) ||
      r.StartsWith("US ", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Normalises OSM ref variants to a canonical hyphenated form so that
  /// "I 90" and "I-90" accumulate into the same bucket.
  /// </summary>
  private static string NormalizeHighway(string r)
  {
    // "I 90" → "I-90",  "US 2" → "US-2",  already-hyphenated pass through.
    if (r.Length > 2 && r[1] == ' ')
      return string.Concat(r.AsSpan(0, 1), "-", r.AsSpan(2));
    if (r.StartsWith("US ", StringComparison.OrdinalIgnoreCase) && r.Length > 3)
      return string.Concat("US-", r.AsSpan(3));
    return r;
  }

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

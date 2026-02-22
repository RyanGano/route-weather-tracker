using route_weather_tracker_service.Data;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Implements geometric pass-on-route matching using the Haversine formula.
/// A pass is considered "on" a route when the shortest distance from the pass
/// coordinates to any segment of the route polyline is within the threshold.
/// </summary>
public class PassLocatorService : IPassLocatorService
{
  public IReadOnlyList<string> FindPassesOnRoute(RouteGeometry geometry, double thresholdKm = 15.0)
  {
    var coords = geometry.Coordinates;
    if (coords.Count < 2) return [];

    // Compute the approximate "route distance from start" for each coordinate pair
    // so we can sort matched passes in travel order.
    var cumulativeKm = BuildCumulativeDistances(coords);

    var matched = new List<(string Id, double PositionKm)>();

    foreach (var pass in PassRegistry.Passes)
    {
      // Fast bounding-box pre-filter before the full segment scan
      var minLat = coords.Min(c => c[1]) - 0.5;
      var maxLat = coords.Max(c => c[1]) + 0.5;
      var minLon = coords.Min(c => c[0]) - 1.0;  // longitude degrees are wider
      var maxLon = coords.Max(c => c[0]) + 1.0;

      if (pass.Latitude < minLat || pass.Latitude > maxLat ||
          pass.Longitude < minLon || pass.Longitude > maxLon)
        continue;

      var (distKm, segIdx) = MinDistanceToPolyline(pass.Latitude, pass.Longitude, coords);
      if (distKm <= thresholdKm)
      {
        // Interpolate position along the route for ordering
        var posKm = cumulativeKm[segIdx] +
            HaversineKm(coords[segIdx][1], coords[segIdx][0], pass.Latitude, pass.Longitude);
        matched.Add((pass.Id, posKm));
      }
    }

    return matched.OrderBy(m => m.PositionKm).Select(m => m.Id).ToList();
  }

  // Returns (distanceKm, segmentStartIndex) for the closest segment
  private static (double DistKm, int SegIdx) MinDistanceToPolyline(
      double lat, double lon,
      IReadOnlyList<IReadOnlyList<double>> coords)
  {
    var minDist = double.MaxValue;
    var minIdx = 0;

    for (var i = 0; i < coords.Count - 1; i++)
    {
      var d = DistanceToSegmentKm(
          lat, lon,
          coords[i][1], coords[i][0],
          coords[i + 1][1], coords[i + 1][0]);

      if (d < minDist)
      {
        minDist = d;
        minIdx = i;
      }
    }

    return (minDist, minIdx);
  }

  /// <summary>
  /// Point-to-line-segment distance using a simple planar projection
  /// (accurate within ±1% for segments shorter than ~200 km).
  /// </summary>
  private static double DistanceToSegmentKm(
      double pLat, double pLon,
      double aLat, double aLon,
      double bLat, double bLon)
  {
    var abLat = bLat - aLat;
    var abLon = bLon - aLon;
    var abLenSq = abLat * abLat + abLon * abLon;

    if (abLenSq < 1e-12)
      return HaversineKm(pLat, pLon, aLat, aLon);

    // Project P onto AB; clamp t to [0,1] to stay on the segment
    var t = Math.Clamp(
        ((pLat - aLat) * abLat + (pLon - aLon) * abLon) / abLenSq,
        0.0, 1.0);

    var closestLat = aLat + t * abLat;
    var closestLon = aLon + t * abLon;
    return HaversineKm(pLat, pLon, closestLat, closestLon);
  }

  private static double[] BuildCumulativeDistances(IReadOnlyList<IReadOnlyList<double>> coords)
  {
    var cum = new double[coords.Count];
    cum[0] = 0;
    for (var i = 1; i < coords.Count; i++)
      cum[i] = cum[i - 1] + HaversineKm(
          coords[i - 1][1], coords[i - 1][0],
          coords[i][1], coords[i][0]);
    return cum;
  }

  internal static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
  {
    const double R = 6371.0;
    var dLat = (lat2 - lat1) * Math.PI / 180.0;
    var dLon = (lon2 - lon1) * Math.PI / 180.0;
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
  }
}

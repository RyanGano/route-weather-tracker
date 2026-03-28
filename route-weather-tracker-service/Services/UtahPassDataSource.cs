using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Utah pass data source.
/// Primary: udotcameras.com processed GeoJSON — proximity search by pass coordinates
/// returns direct UDOT Cctv snapshot image URLs for cameras within 15 km of the pass summit,
/// preferring cameras on the same highway, sorted by distance.
/// Fallback: udotcameras.com homepage HTML scraper.
/// </summary>
public class UtahPassDataSource : IPassDataSource
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<UtahPassDataSource> _logger;

    // Processed GeoJSON of UDOT cameras — each feature has an ImageUrl that returns a live road snapshot
    private const string UdotGeoJsonUrl = "https://udotcameras.com/cctv_locations_processed_classified.geojson";
    // Fallback: udotcameras.com site HTML
    private const string UdotCamerasUrl = "https://udotcameras.com/";

    // Include cameras within this radius (km) of the pass summit
    private const double MaxCameraDistanceKm = 15.0;
    // Return at most this many cameras per pass
    private const int MaxCameras = 5;

    private static readonly IReadOnlySet<string> UtPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "parleys",
            "soldier-summit",
            "sardine",
            "cedar-mountain",
            "beaver-canyon",
            "pine-valley",
        };

    // Pass summit coordinates — used for proximity filtering against the GeoJSON
    private static readonly IReadOnlyDictionary<string, (double Lat, double Lon)> PassCoordinates =
        new Dictionary<string, (double Lat, double Lon)>(StringComparer.OrdinalIgnoreCase)
        {
            ["parleys"]        = (40.6897, -111.7437),
            ["soldier-summit"] = (39.8358, -110.9294),
            ["sardine"]        = (41.6347, -111.8491),
            ["cedar-mountain"] = (37.6315, -112.9621),
            ["beaver-canyon"]  = (38.3980, -112.4614),
            ["pine-valley"]    = (37.2650, -113.6968),
        };

    // Highway keywords used to sort on-highway cameras first
    private static readonly IReadOnlyDictionary<string, string> PassHighwayKeywords =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["parleys"]        = "I-80",
            ["soldier-summit"] = "US-6",
            ["sardine"]        = "US-91",
            ["cedar-mountain"] = "UT-14",
            ["beaver-canyon"]  = "UT-153",
            ["pine-valley"]    = "I-15",
        };

    // Text filters for the HTML scraper fallback only
    private static readonly IReadOnlyDictionary<string, string[]> CameraLocationFilters =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["parleys"]        = ["Parleys", "Parleys Canyon"],
            ["soldier-summit"] = ["Soldier Summit"],
            ["sardine"]        = ["Sardine", "Sardine Summit"],
            ["cedar-mountain"] = ["Cedar Mountain"],
            ["beaver-canyon"]  = ["Beaver Canyon"],
            ["pine-valley"]    = ["Pine Valley"],
        };

    public IReadOnlySet<string> SupportedPassIds => UtPassIds;

    public UtahPassDataSource(IHttpClientFactory httpFactory, ILogger<UtahPassDataSource> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public async Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default)
    {
        if (!PassCoordinates.TryGetValue(passId, out var coords))
            return [];

        PassHighwayKeywords.TryGetValue(passId, out var highway);

        // Primary: GeoJSON proximity search — returns cameras with direct road-snapshot ImageUrls
        var cameras = await GetCamerasFromUdotGeoJsonAsync(coords.Lat, coords.Lon, highway, ct);
        if (cameras.Count > 0)
            return cameras;

        // Last resort: HTML scraper fallback
        if (CameraLocationFilters.TryGetValue(passId, out var filters))
            cameras = await GetCamerasFromUdotCamerasHtmlAsync(passId, filters, ct);

        return cameras;
    }

    private async Task<List<CameraImage>> GetCamerasFromUdotGeoJsonAsync(
        double passLat, double passLon, string? highwayKeyword, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("utah-pass-client");
            using var resp = await client.GetAsync(UdotGeoJsonUrl, ct);
            if (!resp.IsSuccessStatusCode) return [];

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return [];

            var candidates = new List<(double DistKm, bool HighwayMatch, string ImageUrl, string Location, string Id)>();

            foreach (var feat in features.EnumerateArray())
            {
                if (!feat.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
                    continue;

                // Skip disabled cameras
                if (props.TryGetProperty("Status", out var status) &&
                    status.ValueKind == JsonValueKind.String &&
                    !string.Equals(status.GetString(), "Enabled", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Must have a direct image URL
                if (!props.TryGetProperty("ImageUrl", out var iuProp) || iuProp.ValueKind != JsonValueKind.String)
                    continue;
                var imageUrl = iuProp.GetString();
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                // Must have numeric lat/lon
                if (!props.TryGetProperty("Latitude", out var latProp) || latProp.ValueKind != JsonValueKind.Number) continue;
                if (!props.TryGetProperty("Longitude", out var lonProp) || lonProp.ValueKind != JsonValueKind.Number) continue;

                var dist = HaversineKm(passLat, passLon, latProp.GetDouble(), lonProp.GetDouble());
                if (dist > MaxCameraDistanceKm) continue;

                var location = props.TryGetProperty("Location", out var locProp) && locProp.ValueKind == JsonValueKind.String
                    ? (locProp.GetString() ?? string.Empty).Trim()
                    : imageUrl;

                var roadway  = props.TryGetProperty("Roadway", out var rwProp)  && rwProp.ValueKind  == JsonValueKind.String ? rwProp.GetString()  ?? string.Empty : string.Empty;
                var altName  = props.TryGetProperty("ALT_NAME_1A", out var anProp) && anProp.ValueKind == JsonValueKind.String ? anProp.GetString() ?? string.Empty : string.Empty;
                var hwMatch  = !string.IsNullOrEmpty(highwayKeyword) &&
                               (roadway.Contains(highwayKeyword, StringComparison.OrdinalIgnoreCase) ||
                                altName.Contains(highwayKeyword,  StringComparison.OrdinalIgnoreCase));

                var id = props.TryGetProperty("Id", out var idProp) ? idProp.ToString() : StableId(imageUrl);

                candidates.Add((dist, hwMatch, imageUrl, location, id));
            }

            if (candidates.Count == 0) return [];

            return candidates
                .OrderBy(c => c.HighwayMatch ? 0 : 1)
                .ThenBy(c => c.DistKm)
                .Take(MaxCameras)
                .Select(c => new CameraImage
                {
                    CameraId    = c.Id,
                    Description = c.Location,
                    ImageUrl    = c.ImageUrl,
                    FetchedAt   = DateTime.UtcNow
                })
                .ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch UDOT GeoJSON cameras");
            return [];
        }
    }

    private async Task<List<CameraImage>> GetCamerasFromUdotCamerasHtmlAsync(
        string passId, string[] filters, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("utah-pass-client");
            var html = await client.GetStringAsync(UdotCamerasUrl, ct);
            if (string.IsNullOrWhiteSpace(html)) return [];

            var srcRe = new System.Text.RegularExpressions.Regex(
                "src\\s*=\\s*[\"']([^\"']+)[\"']",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var imgMatches = new List<(int Pos, string Url)>();
            foreach (System.Text.RegularExpressions.Match m in srcRe.Matches(html))
            {
                var url = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(url)) continue;
                var norm = url.StartsWith("//") ? "https:" + url
                         : url.StartsWith("/")  ? UdotCamerasUrl.TrimEnd('/') + url
                         : url;
                imgMatches.Add((m.Index, norm));
            }

            var cameras = new List<CameraImage>();
            var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in filters)
            {
                var idx = html.IndexOf(f, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                string? bestUrl  = null;
                int     bestDist = int.MaxValue;
                foreach (var t in imgMatches)
                {
                    var d = Math.Abs(t.Pos - idx);
                    if (d < bestDist) { bestDist = d; bestUrl = t.Url; }
                }

                if (string.IsNullOrWhiteSpace(bestUrl) || seen.Contains(bestUrl)) continue;
                seen.Add(bestUrl);
                cameras.Add(new CameraImage
                {
                    CameraId    = StableId(bestUrl),
                    Description = f,
                    ImageUrl    = bestUrl,
                    FetchedAt   = DateTime.UtcNow
                });
            }

            return cameras;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UDOT cameras HTML fallback failed for {PassId}", passId);
            return [];
        }
    }

    // Haversine great-circle distance in kilometres
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
    }

    private static string StableId(string imageUrl)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(imageUrl));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}

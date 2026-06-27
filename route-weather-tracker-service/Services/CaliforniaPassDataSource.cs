using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// California pass data source.
/// Uses Caltrans' public, key-free CCTV status feeds (one JSON file per district,
/// e.g. https://cwwp2.dot.ca.gov/data/d3/cctv/cctvStatusD03.json) to return the
/// nearest live road-camera snapshots within <see cref="MaxCameraDistanceKm"/> km
/// of each pass summit. Each in-service camera exposes a direct
/// imageData.static.currentImageURL JPEG that Caltrans refreshes every ~1 minute.
///
/// Road conditions / chain control are not yet wired up; passes still fall back to
/// NWS-derived conditions (HasOfficialConditions = false). Chain control can be
/// layered on later via https://www.dot.ca.gov/d{N}/chaincontrol/chaincontrol.json.
/// </summary>
public class CaliforniaPassDataSource : IPassDataSource
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<CaliforniaPassDataSource> _logger;

    private const string DistrictFeedFormat = "https://cwwp2.dot.ca.gov/data/d{0}/cctv/cctvStatusD{1}.json";

    // Include cameras within this radius (km) of the pass summit. Slightly larger
    // than other states so the remote, seasonal Sierra passes (Sonora/Tioga) pick
    // up the nearest US-395 junction camera, which is the only nearby coverage.
    private const double MaxCameraDistanceKm = 25.0;
    private const int MaxCameras = 2;

    // Pass summit coordinates plus the Caltrans district feed(s) that carry the
    // cameras nearest that pass. Most passes sit in one district; the high Sierra
    // passes straddle the D9 (Eastern Sierra) / D10 (Central) boundary, so both
    // are queried and results are merged by distance.
    private static readonly IReadOnlyDictionary<string, (double Lat, double Lon, string[] Districts)> PassMeta =
        new Dictionary<string, (double, double, string[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["mt-shasta"]   = (41.4028, -122.3455, ["02"]),
            ["donner"]      = (39.3224, -120.3287, ["03"]),
            ["echo-summit"] = (38.8108, -120.0348, ["03"]),
            ["cajon"]       = (34.3166, -117.4629, ["08"]),
            ["tehachapi"]   = (35.1308, -118.4383, ["06", "09"]),
            ["monitor"]     = (38.6783, -119.5985, ["10", "09"]),
            ["tioga"]       = (37.9099, -119.2552, ["10", "09"]),
            ["sonora"]      = (38.3294, -119.6264, ["10", "09"]),
        };

    public IReadOnlySet<string> SupportedPassIds { get; } =
        PassMeta.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public CaliforniaPassDataSource(IHttpClientFactory httpFactory, ILogger<CaliforniaPassDataSource> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public async Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default)
    {
        if (!PassMeta.TryGetValue(passId, out var meta))
            return [];

        var client = _httpFactory.CreateClient("california-pass-client");
        var candidates = new List<(double DistKm, CameraImage Cam)>();
        var seenImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var district in meta.Districts)
        {
            foreach (var cam in await GetDistrictCamerasAsync(client, district, meta.Lat, meta.Lon, ct))
            {
                if (seenImages.Add(cam.Cam.ImageUrl))
                    candidates.Add(cam);
            }
        }

        return candidates
            .OrderBy(c => c.DistKm)
            .Take(MaxCameras)
            .Select(c => c.Cam)
            .ToList();
    }

    internal async Task<List<(double DistKm, CameraImage Cam)>> GetDistrictCamerasAsync(
        HttpClient client, string district, double passLat, double passLon, CancellationToken ct)
    {
        var result = new List<(double, CameraImage)>();
        try
        {
            var url = string.Format(DistrictFeedFormat, int.Parse(district), district);
            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return result;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var entry in data.EnumerateArray())
            {
                if (!entry.TryGetProperty("cctv", out var cctv) || cctv.ValueKind != JsonValueKind.Object)
                    continue;

                // Skip out-of-service cameras
                if (cctv.TryGetProperty("inService", out var svc) &&
                    svc.ValueKind == JsonValueKind.String &&
                    !string.Equals(svc.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!cctv.TryGetProperty("location", out var loc) || loc.ValueKind != JsonValueKind.Object)
                    continue;
                if (!TryGetDouble(loc, "latitude", out var lat) || !TryGetDouble(loc, "longitude", out var lon))
                    continue;

                var dist = HaversineKm(passLat, passLon, lat, lon);
                if (dist > MaxCameraDistanceKm) continue;

                var imageUrl = cctv.TryGetProperty("imageData", out var imgData) &&
                               imgData.TryGetProperty("static", out var stat) &&
                               stat.TryGetProperty("currentImageURL", out var iu) &&
                               iu.ValueKind == JsonValueKind.String
                    ? iu.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                var name = loc.TryGetProperty("locationName", out var ln) && ln.ValueKind == JsonValueKind.String
                    ? ln.GetString() ?? string.Empty : string.Empty;
                var nearby = loc.TryGetProperty("nearbyPlace", out var np) && np.ValueKind == JsonValueKind.String
                    ? np.GetString() ?? string.Empty : string.Empty;
                var description = string.IsNullOrWhiteSpace(nearby) ? name : $"{name} ({nearby})";
                var index = cctv.TryGetProperty("index", out var idx) ? idx.ToString() : StableId(imageUrl);

                result.Add((dist, new CameraImage
                {
                    CameraId = $"ca-d{district}-{index}",
                    Description = description,
                    ImageUrl = imageUrl,
                    FetchedAt = DateTime.UtcNow,
                }));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Caltrans CCTV feed for district {District}", district);
        }
        return result;
    }

    private static bool TryGetDouble(JsonElement obj, string prop, out double value)
    {
        value = 0;
        if (!obj.TryGetProperty(prop, out var el)) return false;
        // Caltrans serializes coordinates as JSON strings ("38.481128").
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetDouble(out value),
            JsonValueKind.String => double.TryParse(el.GetString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
    }

    private static string StableId(string imageUrl)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(imageUrl));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}

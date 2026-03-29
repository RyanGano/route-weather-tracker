using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Montana pass data source.
/// Uses the MDT 511 RWIS GeoJSON feed (served via the mt.cdn.iteris-atis.com CDN) to
/// return road-weather camera images for passes within 50 km of the given pass summit.
/// Images are live JPEGs refreshed by MDT every ~2 minutes.
/// </summary>
public class MontanaPassDataSource : IPassDataSource
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MontanaPassDataSource> _logger;

    // MDT 511 RWIS camera GeoJSON — each feature may have a "cameras" array with live image URLs
    private const string RwisGeoJsonUrl =
        "https://mt.cdn.iteris-atis.com/geojson/icons/metadata/icons.rwis.geojson";

    // Include RWIS stations within this radius (km) of the pass summit
    private const double MaxCameraDistanceKm = 50.0;
    // Return at most this many camera images per pass
    private const int MaxCameras = 2;

    private static readonly IReadOnlySet<string> MtPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "marias",
            "logan-pass",
            "chief-joseph-mt",
            "macdonald",
            "rogers-pass-mt",
            "homestake",
        };

    // Pass summit coordinates (from PassRegistry)
    private static readonly IReadOnlyDictionary<string, (double Lat, double Lon)> PassCoordinates =
        new Dictionary<string, (double Lat, double Lon)>(StringComparer.OrdinalIgnoreCase)
        {
            ["marias"]          = (48.3139, -112.9964),
            ["logan-pass"]      = (48.6959, -113.7181),
            ["macdonald"]       = (46.5950, -112.4440),
            ["rogers-pass-mt"]  = (47.0392, -112.5256),
            ["chief-joseph-mt"] = (45.7269, -113.8699),
            ["homestake"]       = (45.9099, -112.7756),
        };

    public IReadOnlySet<string> SupportedPassIds => MtPassIds;

    public MontanaPassDataSource(IHttpClientFactory httpFactory, ILogger<MontanaPassDataSource> logger)
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

        return await GetCamerasFromRwisGeoJsonAsync(coords.Lat, coords.Lon, ct);
    }

    internal async Task<List<CameraImage>> GetCamerasFromRwisGeoJsonAsync(
        double passLat, double passLon, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("montana-pass-client");
            using var resp = await client.GetAsync(RwisGeoJsonUrl, ct);
            if (!resp.IsSuccessStatusCode) return [];

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("features", out var features) ||
                features.ValueKind != JsonValueKind.Array)
                return [];

            // Collect (distance, cameras[]) pairs for each RWIS station within range
            var stationCameras = new List<(double DistKm, JsonElement CamerasArray)>();

            foreach (var feat in features.EnumerateArray())
            {
                if (!feat.TryGetProperty("geometry", out var geom) ||
                    !geom.TryGetProperty("coordinates", out var coords) ||
                    coords.ValueKind != JsonValueKind.Array)
                    continue;

                var coordArr = coords.EnumerateArray().ToArray();
                if (coordArr.Length < 2) continue;

                double lon = coordArr[0].GetDouble();
                double lat = coordArr[1].GetDouble();

                var dist = HaversineKm(passLat, passLon, lat, lon);
                if (dist > MaxCameraDistanceKm) continue;

                if (!feat.TryGetProperty("properties", out var props) ||
                    !props.TryGetProperty("cameras", out var cams) ||
                    cams.ValueKind != JsonValueKind.Array)
                    continue;

                stationCameras.Add((dist, cams));
            }

            // Sort stations by distance and collect up to MaxCameras
            var result = new List<CameraImage>();
            foreach (var (_, cams) in stationCameras.OrderBy(s => s.DistKm))
            {
                foreach (var cam in cams.EnumerateArray())
                {
                    if (result.Count >= MaxCameras) break;

                    var imageUrl = cam.TryGetProperty("image", out var imgProp) && imgProp.ValueKind == JsonValueKind.String
                        ? imgProp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                    var camId = cam.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                        ? idProp.GetString() ?? string.Empty : string.Empty;

                    var description = cam.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                        ? nameProp.GetString() ?? string.Empty : string.Empty;

                    result.Add(new CameraImage
                    {
                        CameraId    = camId,
                        Description = description,
                        ImageUrl    = imageUrl,
                        FetchedAt   = DateTime.UtcNow,
                    });
                }

                if (result.Count >= MaxCameras) break;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Montana RWIS GeoJSON cameras");
            return [];
        }
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
}

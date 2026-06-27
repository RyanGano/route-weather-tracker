using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// New Mexico pass data source.
/// Uses the NM Roads camera API to return road camera images for passes
/// within 50 km of the given pass summit.
/// Images are live JPEGs served by ss.nmroads.com.
/// </summary>
public class NewMexicoPassDataSource : IPassDataSource
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<NewMexicoPassDataSource> _logger;

    private const string CameraInfoUrl = "https://servicev5.nmroads.com/RealMapWAR/GetCameraInfo";
    private const double MaxCameraDistanceKm = 50.0;
    private const int MaxCameras = 2;

    private static readonly IReadOnlySet<string> NmPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "glorieta",
            "tijeras",
            "raton",
            "apache-summit",
            "emory",
        };

    // Pass summit coordinates (from PassRegistry)
    private static readonly IReadOnlyDictionary<string, (double Lat, double Lon)> PassCoordinates =
        new Dictionary<string, (double Lat, double Lon)>(StringComparer.OrdinalIgnoreCase)
        {
            ["glorieta"]      = (35.5218, -105.7700),
            ["tijeras"]       = (35.0698, -106.3855),
            ["raton"]         = (36.9977, -104.5076),
            ["apache-summit"] = (34.1350, -109.8490),
            ["emory"]         = (32.9243, -107.7889),
        };

    public IReadOnlySet<string> SupportedPassIds => NmPassIds;

    public NewMexicoPassDataSource(IHttpClientFactory httpFactory, ILogger<NewMexicoPassDataSource> logger)
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

        return await GetCamerasFromApiAsync(coords.Lat, coords.Lon, ct);
    }

    internal async Task<List<CameraImage>> GetCamerasFromApiAsync(
        double passLat, double passLon, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("newmexico-pass-client");
            using var resp = await client.GetAsync(CameraInfoUrl, ct);
            if (!resp.IsSuccessStatusCode) return [];

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var candidates = new List<(double DistKm, CameraImage Cam)>();

            foreach (var cam in doc.RootElement.EnumerateArray())
            {
                if (cam.TryGetProperty("enabled", out var enabledProp) && !enabledProp.GetBoolean())
                    continue;

                if (!cam.TryGetProperty("snapshotFile", out var snapshotProp)) continue;
                var snapshotUrl = snapshotProp.GetString();
                if (string.IsNullOrWhiteSpace(snapshotUrl)) continue;

                if (!cam.TryGetProperty("lat", out var latProp) ||
                    !cam.TryGetProperty("lon", out var lonProp)) continue;

                var lat = latProp.GetDouble();
                var lon = lonProp.GetDouble();
                var dist = HaversineKm(passLat, passLon, lat, lon);
                if (dist > MaxCameraDistanceKm) continue;

                var title = cam.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? string.Empty : string.Empty;
                var name  = cam.TryGetProperty("name",  out var nameProp)  ? nameProp.GetString()  ?? string.Empty : string.Empty;

                candidates.Add((dist, new CameraImage
                {
                    CameraId    = name,
                    Description = title,
                    ImageUrl    = snapshotUrl,
                    FetchedAt   = DateTime.UtcNow,
                }));
            }

            return candidates
                .OrderBy(c => c.DistKm)
                .Take(MaxCameras)
                .Select(c => c.Cam)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch New Mexico pass cameras");
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

using System.Net.Http.Json;
using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Fetches highway camera images from the Idaho 511 public REST API for
/// Fourth of July Pass and Lookout Pass on I-90.
/// No API key required — images are publicly accessible.
/// API reference: https://511.idaho.gov/
/// </summary>
public class IdahoTransportService : IIdahoTransportService
{
    private readonly HttpClient _http;

    // Idaho 511 endpoints (discovered from page source — no public API key needed)
    // Map icons endpoint returns items with itemId + location [lat, lon]
    private const string MapIconsUrl = "https://511.idaho.gov/map/mapIcons/Cameras";
    // Camera image endpoint: returns a JPEG snapshot directly
    private const string CameraImageBaseUrl = "https://511.idaho.gov/map/Cctv/";

    // Pass center coordinates and search radius (degrees ≈ miles/69)
    private static readonly Dictionary<string, (double Lat, double Lon, double Radius, string Label)> PassCoords
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ["fourth-of-july"] = (47.5333, -116.3667, 0.12, "4th of July Pass"),
            ["lookout"]        = (47.4576, -115.699,  0.10, "Lookout Pass")
        };

    public IdahoTransportService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default)
    {
        if (!PassCoords.TryGetValue(passId, out var pass))
            return [];

        try
        {
            var doc = await _http.GetFromJsonAsync<JsonDocument>(MapIconsUrl, ct);
            if (doc is null) return [];

            // The response has { item1: {icon meta}, item2: [{itemId, location:[lat,lon], ...}] }
            if (!doc.RootElement.TryGetProperty("item2", out var items))
                return [];

            var cameras = new List<CameraImage>();
            int seq = 1;
            foreach (var cam in items.EnumerateArray())
            {
                if (!cam.TryGetProperty("location", out var locArr) || locArr.GetArrayLength() < 2)
                    continue;
                if (!cam.TryGetProperty("itemId", out var idEl))
                    continue;

                var lat = locArr[0].GetDouble();
                var lon = locArr[1].GetDouble();

                if (Math.Abs(lat - pass.Lat) > pass.Radius || Math.Abs(lon - pass.Lon) > pass.Radius)
                    continue;

                var cameraId = idEl.GetString() ?? idEl.GetRawText().Trim('"');
                cameras.Add(new CameraImage
                {
                    CameraId = cameraId,
                    Description = $"{pass.Label} - Camera {seq++}",
                    ImageUrl = $"{CameraImageBaseUrl}{cameraId}",
                    CapturedAt = DateTime.UtcNow
                });
            }

            return cameras;
        }
        catch
        {
            return [];
        }
    }
}

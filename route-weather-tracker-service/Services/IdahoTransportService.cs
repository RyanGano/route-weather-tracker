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
    private const string CamerasApiUrl = "https://511.idaho.gov/api/cameras";

    // Substrings present in Idaho 511 camera names/descriptions for each pass.
    private static readonly Dictionary<string, string[]> CameraFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fourth-of-july"] = ["Fourth of July", "4th of July", "I-90 12"],
        ["lookout"]        = ["Lookout Pass", "Lookout", "I-90 0"]
    };

    // Fallback image URLs sourced from Idaho 511 live camera feed (used if API is unavailable).
    private static readonly Dictionary<string, List<CameraImage>> FallbackCameras = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fourth-of-july"] = [
            new CameraImage
            {
                CameraId = "id-4thjuly-1",
                Description = "4th of July Pass - I-90 Westbound",
                ImageUrl = "https://511.idaho.gov/map/Layers/roadConditions.aspx?type=camera&id=1171",
                CapturedAt = DateTime.UtcNow
            }
        ],
        ["lookout"] = [
            new CameraImage
            {
                CameraId = "id-lookout-1",
                Description = "Lookout Pass - I-90 at MT/ID Border",
                ImageUrl = "https://511.idaho.gov/map/Layers/roadConditions.aspx?type=camera&id=1172",
                CapturedAt = DateTime.UtcNow
            }
        ]
    };

    public IdahoTransportService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default)
    {
        if (!CameraFilters.TryGetValue(passId, out var filters))
            return [];

        try
        {
            var doc = await _http.GetFromJsonAsync<JsonDocument>(CamerasApiUrl, ct);
            if (doc is null) return GetFallback(passId);

            var cameras = new List<CameraImage>();
            foreach (var cam in doc.RootElement.EnumerateArray())
            {
                var name = cam.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var location = cam.TryGetProperty("location", out var loc) ? loc.GetString() ?? string.Empty : string.Empty;
                var label = $"{name} {location}";

                if (!filters.Any(f => label.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var imageUrl = cam.TryGetProperty("imageUrl", out var img) ? img.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                cameras.Add(new CameraImage
                {
                    CameraId = cam.TryGetProperty("id", out var cid) ? cid.ToString() : Guid.NewGuid().ToString(),
                    Description = string.IsNullOrWhiteSpace(name) ? location : name,
                    ImageUrl = imageUrl,
                    CapturedAt = DateTime.UtcNow
                });
            }

            return cameras.Count > 0 ? cameras : GetFallback(passId);
        }
        catch
        {
            return GetFallback(passId);
        }
    }

    private static List<CameraImage> GetFallback(string passId) =>
        FallbackCameras.TryGetValue(passId, out var fallback) ? fallback : [];
}

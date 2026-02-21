using System.Net.Http.Json;
using System.Text.Json;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Fetches mountain pass conditions and cameras from the WSDOT Traveler Information API.
/// API key (WsdotApiKey) is resolved from Azure Key Vault via IConfiguration.
/// Documentation: https://wsdot.wa.gov/traffic/api/
/// </summary>
public class WsdotService : IWsdotService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string BaseUrl = "https://wsdot.wa.gov/Traffic/api";

    // WSDOT Mountain Pass IDs (from the MountainPassConditions API)
    private static readonly Dictionary<string, int> PassIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["snoqualmie"] = 1   // Snoqualmie Pass I-90 (PassConditionID=1 in WSDOT data)
    };

    // Camera location substrings used to filter the HighwayCameras response
    private static readonly Dictionary<string, string[]> CameraLocationFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["snoqualmie"] = ["Snoqualmie", "I-90 @ MP 52", "Summit"]
    };

    public WsdotService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["WsdotApiKey"]
            ?? throw new InvalidOperationException("WsdotApiKey secret not found. Ensure it is set in Azure Key Vault.");
    }

    public async Task<PassCondition?> GetPassConditionAsync(string passId, CancellationToken ct = default)
    {
        if (!PassIdMap.TryGetValue(passId, out var wsdotId))
            return null;

        var url = $"{BaseUrl}/MountainPassConditions/GetMountainPassConditionAsJson?AccessCode={_apiKey}&PassConditionID={wsdotId}";

        try
        {
            var doc = await _http.GetFromJsonAsync<JsonDocument>(url, ct);
            if (doc is null) return null;

            var root = doc.RootElement;
            var restriction = ParseRestriction(root.GetProperty("TravelAdvisoryActive").GetBoolean(),
                                               root.TryGetProperty("RestrictionOne", out var r1) ? r1 : (JsonElement?)null,
                                               root.TryGetProperty("RestrictionTwo", out var r2) ? r2 : (JsonElement?)null);

            return new PassCondition
            {
                PassId = passId,
                RoadCondition = root.TryGetProperty("RoadCondition", out var rc) ? rc.GetString() ?? "Unknown" : "Unknown",
                WeatherCondition = root.TryGetProperty("WeatherCondition", out var wc) ? wc.GetString() ?? "Unknown" : "Unknown",
                TemperatureFahrenheit = root.TryGetProperty("TemperatureInFahrenheit", out var temp) ? temp.GetInt32() : 0,
                EastboundRestriction = restriction.Eastbound,
                WestboundRestriction = restriction.Westbound,
                LastUpdated = root.TryGetProperty("DateUpdated", out var dt)
                    ? DateTime.TryParse(dt.GetString(), out var parsed) ? parsed : DateTime.UtcNow
                    : DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default)
    {
        if (!CameraLocationFilters.TryGetValue(passId, out var filters))
            return [];

        var url = $"{BaseUrl}/HighwayCameras/GetCameraInventoryAsJson?AccessCode={_apiKey}";

        try
        {
            var doc = await _http.GetFromJsonAsync<JsonDocument>(url, ct);
            if (doc is null) return [];

            var cameras = new List<CameraImage>();
            foreach (var cam in doc.RootElement.EnumerateArray())
            {
                var description = cam.TryGetProperty("Title", out var title) ? title.GetString() ?? string.Empty : string.Empty;
                if (!filters.Any(f => description.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var imageUrl = cam.TryGetProperty("ImageURL", out var img) ? img.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                cameras.Add(new CameraImage
                {
                    CameraId = cam.TryGetProperty("CameraID", out var cid) ? cid.ToString() : Guid.NewGuid().ToString(),
                    Description = description,
                    ImageUrl = imageUrl,
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

    private static (TravelRestriction Eastbound, TravelRestriction Westbound) ParseRestriction(
        bool advisoryActive, JsonElement? restriction1, JsonElement? restriction2)
    {
        if (!advisoryActive)
            return (TravelRestriction.None, TravelRestriction.None);

        var eb = ParseSingleRestriction(restriction1);
        var wb = ParseSingleRestriction(restriction2);
        return (eb, wb);
    }

    private static TravelRestriction ParseSingleRestriction(JsonElement? r)
    {
        if (r is null) return TravelRestriction.None;
        var text = r.Value.TryGetProperty("RestrictionText", out var rt) ? rt.GetString() ?? "" : "";
        return text.ToLowerInvariant() switch
        {
            var t when t.Contains("chain") => TravelRestriction.ChainsRequired,
            var t when t.Contains("traction") || t.Contains("tires") => TravelRestriction.TiresOrTraction,
            var t when t.Contains("closed") => TravelRestriction.Closed,
            _ => TravelRestriction.None
        };
    }
}

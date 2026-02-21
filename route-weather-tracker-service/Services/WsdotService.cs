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

  // Correct REST .svc endpoint URLs (see https://wsdot.wa.gov/traffic/api/)
  private const string CamerasUrl = "https://wsdot.wa.gov/Traffic/api/HighwayCameras/HighwayCamerasREST.svc/GetCamerasAsJson";
  // Note: WSDOT has a typo in the method name — "AsJon" not "AsJson"
  private const string PassConditionUrl = "https://wsdot.wa.gov/Traffic/api/MountainPassConditions/MountainPassConditionsREST.svc/GetMountainPassConditionAsJon";

  // WSDOT Mountain Pass IDs — verified from GetMountainPassConditionsAsJson
  private static readonly Dictionary<string, int> PassIdMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["snoqualmie"] = 11,  // Snoqualmie Pass I-90 (MountainPassId=11 in WSDOT data)
    ["stevens-pass"] = 2    // Stevens Pass US-2 (MountainPassId=2 in WSDOT data)
  };

  // Camera Title substrings used to filter the HighwayCameras response
  private static readonly Dictionary<string, string[]> CameraLocationFilters = new(StringComparer.OrdinalIgnoreCase)
  {
    ["snoqualmie"] = ["Snoqualmie", "I-90 at MP 52", "I-90 at MP 53"],
    ["stevens-pass"] = ["Stevens Pass Summit", "MP 64.6", "MP 65"]
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

    var url = $"{PassConditionUrl}?AccessCode={_apiKey}&PassConditionID={wsdotId}";

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
        EastboundRestrictionText = restriction.EastboundText,
        WestboundRestrictionText = restriction.WestboundText,
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

    var url = $"{CamerasUrl}?AccessCode={_apiKey}";

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

  private static (TravelRestriction Eastbound, TravelRestriction Westbound, string EastboundText, string WestboundText) ParseRestriction(
      bool advisoryActive, JsonElement? restriction1, JsonElement? restriction2)
  {
    if (!advisoryActive)
      return (TravelRestriction.None, TravelRestriction.None, string.Empty, string.Empty);

    var (ebEnum, ebText) = ParseSingleRestriction(restriction1);
    var (wbEnum, wbText) = ParseSingleRestriction(restriction2);
    return (ebEnum, wbEnum, ebText, wbText);
  }

  private static (TravelRestriction Restriction, string Text) ParseSingleRestriction(JsonElement? r)
  {
    if (r is null) return (TravelRestriction.None, string.Empty);
    var text = r.Value.TryGetProperty("RestrictionText", out var rt) ? rt.GetString() ?? "" : "";
    var restriction = text.ToLowerInvariant() switch
    {
      var t when t.Contains("chain") => TravelRestriction.ChainsRequired,
      var t when t.Contains("traction") || t.Contains("tires") => TravelRestriction.TiresOrTraction,
      var t when t.Contains("closed") => TravelRestriction.Closed,
      _ => TravelRestriction.None
    };
    return (restriction, text);
  }
}

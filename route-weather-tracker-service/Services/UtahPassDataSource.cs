using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Utah pass data source. Uses the public UDOT ArcGIS Road_Conditions FeatureServer
/// to discover cameras for Utah passes. The ArcGIS REST service requires no key.
/// </summary>
public class UtahPassDataSource : IPassDataSource
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<UtahPassDataSource> _logger;

    // FeatureServer query that returns road condition features (includes fields)
    private const string ArcGisQueryUrl =
            "https://services.arcgis.com/Vl0VBqVpJSB0FpLN/arcgis/rest/services/Road_Conditions/FeatureServer/0/query?where=1%3D1&outFields=*&f=json";

    // Fallback public site that aggregates UDOT camera links; used only if ArcGIS finds no images
    private const string UdotCamerasUrl = "https://udotcameras.com/";
    // Processed GeoJSON of UDOT cameras used by udotcameras frontend
    private const string UdotGeoJsonUrl = "https://udotcameras.com/cctv_locations_processed_classified.geojson";

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

    // Simple mapping of passId -> substrings expected to appear in the ArcGIS feature
    // attributes (field values like NAME, ROUTE, LOCATION, or DESCRIPTION).
    private static readonly Dictionary<string, string[]> CameraLocationFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["parleys"] = ["Parleys", "Parleys Canyon", "I-80 Parleys"],
        ["soldier-summit"] = ["Soldier Summit", "Soldier Summit"],
        ["sardine"] = ["Sardine", "Sardine Summit"],
        ["cedar-mountain"] = ["Cedar Mountain", "Cedar Mountain Summit"],
        ["beaver-canyon"] = ["Beaver Canyon", "Beaver Canyon Summit"],
        ["pine-valley"] = ["Pine Valley", "Pine Valley Summit"],
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
        if (!CameraLocationFilters.TryGetValue(passId, out var filters))
            return new List<CameraImage>();

        var client = _httpFactory.CreateClient("utah-pass-client");
        try
        {
            using var resp = await client.GetAsync(ArcGisQueryUrl, ct);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return new List<CameraImage>();

            var cameras = new List<CameraImage>();

            foreach (var feat in features.EnumerateArray())
            {
                if (!feat.TryGetProperty("attributes", out var attr) || attr.ValueKind != JsonValueKind.Object)
                    continue;

                // Build a searchable text blob from attribute string values
                var searchable = new List<string>();
                foreach (var prop in attr.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        searchable.Add(prop.Value.GetString() ?? string.Empty);
                }
                var searchableText = string.Join(" ", searchable);

                if (!filters.Any(f => searchableText.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Try to find a URL in attributes that looks like an image
                string? imageUrl = null;
                foreach (var prop in attr.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String) continue;
                    var s = prop.Value.GetString() ?? string.Empty;
                    if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        var lower = s.ToLowerInvariant();
                        if (lower.Contains(".jpg") || lower.Contains(".jpeg") || lower.Contains(".png") || lower.Contains("/cameras/") || lower.Contains("/camera/"))
                        {
                            imageUrl = s;
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(imageUrl))
                    continue;

                // Description: pick the most likely name/title field
                var description = attr.TryGetProperty("CAMERA_NAME", out var cn) ? cn.GetString() ?? string.Empty :
                                                    attr.TryGetProperty("NAME", out var n) ? n.GetString() ?? string.Empty :
                                                    attr.TryGetProperty("LOCATION", out var loc) ? loc.GetString() ?? string.Empty :
                                                    searchableText;

                var cameraId = attr.TryGetProperty("OBJECTID", out var oid) ? oid.ToString() : StableId(imageUrl);

                cameras.Add(new CameraImage
                {
                    CameraId = cameraId ?? StableId(imageUrl),
                    Description = description,
                    ImageUrl = imageUrl,
                    FetchedAt = DateTime.UtcNow
                });
            }

            if (cameras.Count == 0)
            {
                // Try fallback to udotcameras.com when ArcGIS returns no image URLs
                var fallback = await GetCamerasFromUdotCamerasAsync(passId, filters, ct);
                if (fallback?.Count > 0)
                    return fallback;
            }

            return cameras;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching UDOT ArcGIS data for pass {PassId}", passId);
            return new List<CameraImage>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse UDOT ArcGIS response for pass {PassId}", passId);
            return new List<CameraImage>();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching UDOT cameras for pass {PassId}", passId);
            return new List<CameraImage>();
        }
    }

    // If ArcGIS did not return any cameras for a pass, try the public udotcameras.com page
    // and heuristically locate nearby image URLs by searching for the pass filter text.
    private async Task<List<CameraImage>> GetCamerasFromUdotCamerasAsync(string passId, string[] filters, CancellationToken ct)
    {
        try
        {
            // First try the processed GeoJSON which contains structured camera entries
            var geo = await GetCamerasFromUdotGeoJsonAsync(filters, ct);
            if (geo?.Count > 0)
                return geo;

            // Fallback: try scraping homepage for image tags
            var client = _httpFactory.CreateClient("utah-pass-client");
            var html = await client.GetStringAsync(UdotCamerasUrl, ct);
            if (string.IsNullOrWhiteSpace(html))
                return new List<CameraImage>();

            // Find src attributes and choose nearest to filter text
            var srcRe = new System.Text.RegularExpressions.Regex("src\\s*=\\s*[\"']([^\"']+)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var imgMatches = new List<(int pos, string url)>();
            foreach (System.Text.RegularExpressions.Match m in srcRe.Matches(html))
            {
                var url = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(url)) continue;
                var norm = url.StartsWith("//") ? "https:" + url : url.StartsWith("/") ? (UdotCamerasUrl.TrimEnd('/') + url) : url;
                imgMatches.Add((m.Index, norm));
            }

            var cameras = new List<CameraImage>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in filters)
            {
                var idx = html.IndexOf(f, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                int bestDist = int.MaxValue;
                string? bestUrl = null;
                // prefer images within a local window
                foreach (var t in imgMatches)
                {
                    var d = Math.Abs(t.pos - idx);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestUrl = t.url;
                    }
                }

                if (string.IsNullOrWhiteSpace(bestUrl)) continue;
                if (seen.Contains(bestUrl)) continue;
                seen.Add(bestUrl);

                cameras.Add(new CameraImage
                {
                    CameraId = StableId(bestUrl),
                    Description = f,
                    ImageUrl = bestUrl,
                    FetchedAt = DateTime.UtcNow
                });
            }

            return cameras;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UDOT cameras fallback failed for {PassId}", passId);
            return new List<CameraImage>();
        }
    }

    private async Task<List<CameraImage>> GetCamerasFromUdotGeoJsonAsync(string[] filters, CancellationToken ct)
    {
        try
        {
            var client = _httpFactory.CreateClient("utah-pass-client");
            using var resp = await client.GetAsync(UdotGeoJsonUrl, ct);
            if (!resp.IsSuccessStatusCode) return new List<CameraImage>();
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                return new List<CameraImage>();

            var cameras = new List<CameraImage>();
            foreach (var feat in features.EnumerateArray())
            {
                if (!feat.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
                    continue;

                // Build searchable text
                var searchable = new List<string>();
                foreach (var p in props.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.String)
                        searchable.Add(p.Value.GetString() ?? string.Empty);
                }
                var searchableText = string.Join(' ', searchable);
                if (!filters.Any(f => searchableText.Contains(f, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Try common image url fields
                string? imageUrl = null;
                if (props.TryGetProperty("ImageUrl", out var iu) && iu.ValueKind == JsonValueKind.String)
                    imageUrl = iu.GetString();
                else if (props.TryGetProperty("ImageURL", out var iu2) && iu2.ValueKind == JsonValueKind.String)
                    imageUrl = iu2.GetString();
                else if (props.TryGetProperty("Url", out var u3) && u3.ValueKind == JsonValueKind.String)
                    imageUrl = u3.GetString();

                if (string.IsNullOrWhiteSpace(imageUrl))
                    continue;

                var desc = props.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String ? nm.GetString() ?? string.Empty : searchableText;
                cameras.Add(new CameraImage { CameraId = StableId(imageUrl), Description = desc, ImageUrl = imageUrl, FetchedAt = DateTime.UtcNow });
            }

            return cameras;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch udot geojson");
            return new List<CameraImage>();
        }
    }

    private static string StableId(string imageUrl)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(imageUrl));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Utah passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official UDOT conditions, use the UDOT ArcGIS REST service (no key):
///   GET https://services.arcgis.com/Vl0VBqVpJSB0FpLN/arcgis/rest/services/
///       Road_Conditions/FeatureServer/0/query?where=1%3D1&amp;outFields=*&amp;f=json
/// </summary>
public class UtahPassDataSource : IPassDataSource
{
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

    public IReadOnlySet<string> SupportedPassIds => UtPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

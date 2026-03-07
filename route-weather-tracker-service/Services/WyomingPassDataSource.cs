using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Wyoming passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official WYDOT conditions, use the WYDOT ArcGIS REST service (no key):
///   GET https://services2.arcgis.com/XAiUZpe3bqC8OiNm/arcgis/rest/services/
///       WYDOT_Road_Conditions/FeatureServer/0/query?where=1%3D1&amp;outFields=*&amp;f=json
/// Match each pass to the nearest road condition segment by lat/lon.
/// </summary>
public class WyomingPassDataSource : IPassDataSource
{
    private static readonly IReadOnlySet<string> WyPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "teton-pass",
            "togwotee",
            "snowy-range",
            "south-pass",
            "powder-river",
            "beartooth",
        };

    public IReadOnlySet<string> SupportedPassIds => WyPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

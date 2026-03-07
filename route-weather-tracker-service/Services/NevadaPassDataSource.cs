using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Nevada passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official NDOT conditions, use the Nevada Roads ArcGIS service (no key):
///   GET https://services.arcgis.com/8lRhdTsQyJpO52F1/arcgis/rest/services/
///       Open511_Incidents/FeatureServer/0/query?where=1%3D1&amp;outFields=*&amp;f=json
/// </summary>
public class NevadaPassDataSource : IPassDataSource
{
    private static readonly IReadOnlySet<string> NvPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "spooner",
            "mount-rose",
            "golconda",
            "palisade",
        };

    public IReadOnlySet<string> SupportedPassIds => NvPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

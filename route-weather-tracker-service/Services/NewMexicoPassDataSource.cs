using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for New Mexico passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official NMDOT conditions, use the NM Roads ArcGIS service (no key):
///   GET https://nmroads.com/arcgis/rest/services/NMDOT/RoadConditions/
///       MapServer/0/query?where=1%3D1&amp;outFields=*&amp;f=json
/// </summary>
public class NewMexicoPassDataSource : IPassDataSource
{
    private static readonly IReadOnlySet<string> NmPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "glorieta",
            "tijeras",
            "raton",
            "apache-summit",
            "emory",
        };

    public IReadOnlySet<string> SupportedPassIds => NmPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

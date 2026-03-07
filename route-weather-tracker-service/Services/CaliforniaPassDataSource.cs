using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for California passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// Real Caltrans data sources (no key required) that can be layered on later:
/// - Chain control: GET https://www.dot.ca.gov/d3/chaincontrol/chaincontrol.json
/// - CCTV cameras:  GET https://cwwp2.dot.ca.gov/tools/cctv/cameras.json
///   (filter by District 3 for I-80/US-50 Sierra Nevada passes)
/// Map chain control codes: R1=chains/snow-tires, R2=chains required, R3=closed.
/// </summary>
public class CaliforniaPassDataSource : IPassDataSource
{
    private static readonly IReadOnlySet<string> CaPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "donner",
            "echo-summit",
            "cajon",
            "tehachapi",
            "monitor",
            "tioga",
            "sonora",
            "mt-shasta",
        };

    public IReadOnlySet<string> SupportedPassIds => CaPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

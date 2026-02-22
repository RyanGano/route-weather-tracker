using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Montana passes.
/// Currently returns no conditions and no cameras — all passes fall back to
/// OpenWeather for weather data (<see cref="PassInfo.HasOfficialConditions"/> is
/// false on every MT pass until MDT 511 integration is complete).
///
/// To add real data: implement <c>IMtdService</c> / <c>MtdService</c> that parses
/// the MDT 511 XML condition feed (https://www.mdt.mt.gov/travinfo/maps/511.shtml),
/// inject it here, and set <c>HasOfficialConditions = true</c> in PassRegistry.
/// </summary>
public class MontanaPassDataSource : IPassDataSource
{
    // All Montana pass IDs managed by this data source.
    // Conditions and cameras will be populated when MDT integration is added.
    private static readonly IReadOnlySet<string> MtPassIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "marias",
            "logan-pass",
            "chief-joseph-mt",
            "macdonald",
            "rogers-pass-mt",
            "homestake",
        };

    public IReadOnlySet<string> SupportedPassIds => MtPassIds;

    public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult<PassCondition?>(null);

    public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
        Task.FromResult(new List<CameraImage>());
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Oregon passes.
/// Passes are matched by NWS weather data (HasOfficialConditions = false).
/// A full ODOT TripCheck integration (https://api.tripcheck.com/api/) can be
/// layered on top later — no key is required, but the road-segment → pass
/// mapping logic needs per-pass milepost configuration.
/// </summary>
public class OregonPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> OrPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
            "santiam",
            "willamette",
            "siskiyou",
            "deadman",
            "mckenzie",
            "government-camp",
      };

  public IReadOnlySet<string> SupportedPassIds => OrPassIds;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult<PassCondition?>(null);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult(new List<CameraImage>());
}

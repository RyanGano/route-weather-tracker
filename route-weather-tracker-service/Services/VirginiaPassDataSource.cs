using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Virginia Appalachian passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official VDOT 511 conditions, register for a free API key at
/// https://developer.511Virginia.org, store it as VDOT-ApiKey in Azure Key Vault,
/// and call:
///   GET https://www.511virginia.org/api/v1/incidents?apiKey={key}
///   GET https://www.511virginia.org/api/v1/cameras?apiKey={key}
/// </summary>
public class VirginiaPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> VaPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
            "afton-mountain",
            "rockfish-gap",
            "fancy-gap",
            "shenandoah-gap",
      };

  public IReadOnlySet<string> SupportedPassIds => VaPassIds;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult<PassCondition?>(null);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult(new List<CameraImage>());
}

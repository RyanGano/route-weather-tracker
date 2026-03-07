using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for North Carolina and Tennessee mountain passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To add official condition data:
/// - NC (NCDOT): GET https://tims.ncdot.gov/tims/api/v1/incidents?format=json (no key)
/// - TN (TDOT):  GET https://developer.tn511.com/api/ (free API key required;
///   store as TDOT-ApiKey in Azure Key Vault)
/// - GSMNP road closures: https://www.nps.gov/grsm/planyourvisit/parkreads.htm
/// </summary>
public class NcTnPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> NcTnPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
            "newfound-gap",
            "cherokee",
            "clinch-mountain",
            "santeelah",
            "clingmans-dome",
      };

  public IReadOnlySet<string> SupportedPassIds => NcTnPassIds;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult<PassCondition?>(null);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult(new List<CameraImage>());
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Scaffold IPassDataSource for Colorado passes.
/// Passes fall back to NWS weather data (HasOfficialConditions = false).
///
/// To enable official CDOT conditions, register for a free API key at
/// https://manage-api.cotrip.org/login, store it as CDOT-ApiKey in Azure Key Vault,
/// inject it here, and implement the COtrip road conditions fetch:
///   GET https://data.cotrip.org/api/v1/roadConditions?apiKey={key}
/// Match each pass to the nearest segment by travel_center_point distance.
/// </summary>
public class ColoradoPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> CoPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
            "vail-pass",
            "eisenhower-tunnel",
            "loveland-pass",
            "berthoud-pass",
            "rabbit-ears",
            "monarch-pass",
            "wolf-creek",
            "red-mountain",
            "la-veta",
            "cameron-pass",
            "hoosier-pass",
            "kenosha-pass",
            "fremont-pass",
            "poncha-pass",
      };

  public IReadOnlySet<string> SupportedPassIds => CoPassIds;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult<PassCondition?>(null);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct = default) =>
      Task.FromResult(new List<CameraImage>());
}

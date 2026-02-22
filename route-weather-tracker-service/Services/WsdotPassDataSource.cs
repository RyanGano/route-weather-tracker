using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Adapts IWsdotService as an IPassDataSource for Washington State passes
/// (Snoqualmie Pass I-90 and Stevens Pass US-2).
/// </summary>
public class WsdotPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> _supported =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "snoqualmie", "stevens-pass" };

  private readonly IWsdotService _wsdot;

  public WsdotPassDataSource(IWsdotService wsdot) => _wsdot = wsdot;

  public IReadOnlySet<string> SupportedPassIds => _supported;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct) =>
      _wsdot.GetPassConditionAsync(passId, ct);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct) =>
      _wsdot.GetPassCamerasAsync(passId, ct);
}

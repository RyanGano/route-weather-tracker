using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Adapts IIdahoTransportService as an IPassDataSource for Idaho passes
/// (Fourth of July Pass I-90 and Lookout Pass I-90).
/// Idaho does not provide road condition data, only cameras.
/// </summary>
public class IdahoPassDataSource : IPassDataSource
{
  private static readonly IReadOnlySet<string> _supported =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fourth-of-july", "lookout" };

  private readonly IIdahoTransportService _idaho;

  public IdahoPassDataSource(IIdahoTransportService idaho) => _idaho = idaho;

  public IReadOnlySet<string> SupportedPassIds => _supported;

  public Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct) =>
      Task.FromResult<PassCondition?>(null);

  public Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct) =>
      _idaho.GetPassCamerasAsync(passId, ct);
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Abstracts a data source for a specific set of mountain passes.
/// Implementations exist for WSDOT (WA passes) and Idaho 511 (ID/MT passes).
/// Adding support for a new state requires only a new implementation — no changes
/// to PassAggregatorService.
/// </summary>
public interface IPassDataSource
{
  /// <summary>Pass IDs that this source can supply data for.</summary>
  IReadOnlySet<string> SupportedPassIds { get; }

  /// <summary>
  /// Returns current road conditions for the pass, or <c>null</c> if this
  /// source does not provide official condition data (e.g. Idaho passes).
  /// </summary>
  Task<PassCondition?> GetConditionAsync(string passId, CancellationToken ct);

  /// <summary>Returns live camera images for the pass.</summary>
  Task<List<CameraImage>> GetCamerasAsync(string passId, CancellationToken ct);
}

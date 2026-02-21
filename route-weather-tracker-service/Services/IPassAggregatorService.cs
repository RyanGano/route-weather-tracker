using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

public interface IPassAggregatorService
{
  Task<List<PassSummary>> GetAllPassesAsync(CancellationToken ct = default);
  Task<List<PassSummary>> GetPassesAsync(IEnumerable<string> passIds, CancellationToken ct = default);
  Task<PassSummary?> GetPassAsync(string passId, CancellationToken ct = default);
}

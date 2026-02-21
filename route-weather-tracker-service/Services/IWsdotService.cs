using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

public interface IWsdotService
{
    Task<PassCondition?> GetPassConditionAsync(string passId, CancellationToken ct = default);
    Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default);
}

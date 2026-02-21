using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

public interface IIdahoTransportService
{
  Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default);
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

public interface INwsService
{
  Task<PassWeatherForecast?> GetForecastAsync(string passId, double latitude, double longitude, CancellationToken ct = default);
}

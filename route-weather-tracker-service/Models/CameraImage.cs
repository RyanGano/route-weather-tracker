namespace route_weather_tracker_service.Models;

public class CameraImage
{
  public string CameraId { get; init; } = string.Empty;
  public string Description { get; init; } = string.Empty;
  public string ImageUrl { get; init; } = string.Empty;
  public DateTime CapturedAt { get; init; }
}

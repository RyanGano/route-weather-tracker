namespace route_weather_tracker_service.Models;

public class CameraImage
{
    public string CameraId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}

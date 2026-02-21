namespace route_weather_tracker_service.Models;

public class PassSummary
{
    public PassInfo Info { get; set; } = new();
    public PassCondition? Condition { get; set; }
    public List<CameraImage> Cameras { get; set; } = new();
    public PassWeatherForecast? Weather { get; set; }
}

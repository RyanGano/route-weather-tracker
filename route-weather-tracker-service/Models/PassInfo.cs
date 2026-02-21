namespace route_weather_tracker_service.Models;

public class PassInfo
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Highway { get; set; } = string.Empty;
  public int ElevationFeet { get; set; }
  public double Latitude { get; set; }
  public double Longitude { get; set; }
  public string State { get; set; } = string.Empty;
  public string OfficialUrl { get; set; } = string.Empty;
}

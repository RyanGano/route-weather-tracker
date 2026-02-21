namespace route_weather_tracker_service.Models;

public enum TravelRestriction
{
    None,
    TiresOrTraction,
    ChainsRequired,
    Closed
}

public class PassCondition
{
    public string PassId { get; set; } = string.Empty;
    public string RoadCondition { get; set; } = string.Empty;
    public string WeatherCondition { get; set; } = string.Empty;
    public TravelRestriction EastboundRestriction { get; set; }
    public TravelRestriction WestboundRestriction { get; set; }
    public int TemperatureFahrenheit { get; set; }
    public DateTime LastUpdated { get; set; }
}

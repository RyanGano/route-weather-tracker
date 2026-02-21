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
  /// <summary>Raw restriction text from the data source (e.g. "Traction Tires Advised, Oversize Vehicles Prohibited").</summary>
  public string EastboundRestrictionText { get; set; } = string.Empty;
  public string WestboundRestrictionText { get; set; } = string.Empty;
  public int TemperatureFahrenheit { get; set; }
  public DateTime LastUpdated { get; set; }
}

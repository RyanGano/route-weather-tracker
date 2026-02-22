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
  public string PassId { get; init; } = string.Empty;
  public string RoadCondition { get; init; } = string.Empty;
  public string WeatherCondition { get; init; } = string.Empty;
  public TravelRestriction EastboundRestriction { get; init; }
  public TravelRestriction WestboundRestriction { get; init; }
  /// <summary>Raw restriction text from the data source (e.g. "Traction Tires Advised, Oversize Vehicles Prohibited").</summary>
  public string EastboundRestrictionText { get; init; } = string.Empty;
  public string WestboundRestrictionText { get; init; } = string.Empty;
  public int TemperatureFahrenheit { get; init; }
  public DateTime LastUpdated { get; init; }
}

using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Data;

public static class PassRegistry
{
    public static readonly IReadOnlyList<PassInfo> Passes = new List<PassInfo>
    {
        new PassInfo
        {
            Id = "snoqualmie",
            Name = "Snoqualmie Pass",
            Highway = "I-90",
            ElevationFeet = 3022,
            Latitude = 47.4245,
            Longitude = -121.4116,
            State = "WA",
            OfficialUrl = "https://wsdot.wa.gov/travel/roads-bridges/mountain-pass-reports"
        },
        new PassInfo
        {
            Id = "fourth-of-july",
            Name = "Fourth of July Pass",
            Highway = "I-90",
            ElevationFeet = 3081,
            Latitude = 47.5333,
            Longitude = -116.3667,
            State = "ID",
            OfficialUrl = "https://511.idaho.gov/"
        },
        new PassInfo
        {
            Id = "lookout",
            Name = "Lookout Pass",
            Highway = "I-90",
            ElevationFeet = 4738,
            Latitude = 47.4576,
            Longitude = -115.6990,
            State = "MT/ID",
            OfficialUrl = "https://www.mdt.mt.gov/travinfo/"
        }
    };

    public static PassInfo? GetById(string id) =>
        Passes.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

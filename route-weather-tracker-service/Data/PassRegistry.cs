using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Data;

public static class PassRegistry
{
    public static readonly IReadOnlyList<PassInfo> Passes =
    [
        new PassInfo
        {
            Id = "snoqualmie",
            Name = "Snoqualmie Pass",
            Highway = "I-90",
            ElevationFeet = 3022,
            Latitude = 47.4245,
            Longitude = -121.4116,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/snoqualmie"
        },
        new PassInfo
        {
            Id = "stevens-pass",
            Name = "Stevens Pass",
            Highway = "US-2",
            ElevationFeet = 4061,
            Latitude = 47.7447,
            Longitude = -121.0891,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/stevens"
        },
        new PassInfo
        {
            Id = "cayuse",
            Name = "Cayuse Pass",
            Highway = "WA-123",
            ElevationFeet = 4694,
            Latitude = 46.8706,
            Longitude = -121.5445,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/cayuse"
        },
        new PassInfo
        {
            Id = "white",
            Name = "White Pass",
            Highway = "US-12",
            ElevationFeet = 4500,
            Latitude = 46.6388,
            Longitude = -121.3988,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/white"
        },
        new PassInfo
        {
            Id = "washington-pass",
            Name = "Washington Pass",
            Highway = "WA-20",
            ElevationFeet = 5477,
            Latitude = 48.5195,
            Longitude = -120.6653,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/washington"
        },
        new PassInfo
        {
            Id = "rainy-pass",
            Name = "Rainy Pass",
            Highway = "WA-20",
            ElevationFeet = 4855,
            Latitude = 48.5195,
            Longitude = -120.7364,
            State = "WA",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "sherman",
            Name = "Sherman Pass",
            Highway = "WA-20",
            ElevationFeet = 5575,
            Latitude = 48.6030,
            Longitude = -118.4630,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/sherman"
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
            OfficialUrl = "https://511.idaho.gov/List/Cameras?search=july"
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
            OfficialUrl = "https://511.idaho.gov/List/Cameras?search=lookout"
        },

        // ── Idaho — OpenWeather-only (camera IDs unconfirmed) ────────────────────
        new PassInfo
        {
            Id = "lolo",
            Name = "Lolo Pass",
            Highway = "US-12",
            ElevationFeet = 5233,
            Latitude = 46.6494,
            Longitude = -114.5983,
            State = "ID/MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "lost-trail",
            Name = "Lost Trail Pass",
            Highway = "US-93",
            ElevationFeet = 6995,
            Latitude = 45.6800,
            Longitude = -113.9500,
            State = "ID/MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "banner-summit",
            Name = "Banner Summit",
            Highway = "ID-21",
            ElevationFeet = 6989,
            Latitude = 44.2608,
            Longitude = -114.9731,
            State = "ID",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "galena-summit",
            Name = "Galena Summit",
            Highway = "ID-75",
            ElevationFeet = 8701,
            Latitude = 43.8742,
            Longitude = -114.6978,
            State = "ID",
            HasOfficialConditions = false
        },

        // ── Montana — OpenWeather-only until MDT 511 integration ──────────────────────
        new PassInfo
        {
            Id = "marias",
            Name = "Marias Pass",
            Highway = "US-2",
            ElevationFeet = 5213,
            Latitude = 48.3139,
            Longitude = -112.9964,
            State = "MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "logan-pass",
            Name = "Logan Pass",
            Highway = "US-89",
            ElevationFeet = 6646,
            Latitude = 48.6959,
            Longitude = -113.7181,
            State = "MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "macdonald",
            Name = "MacDonald Pass",
            Highway = "US-12",
            ElevationFeet = 6325,
            Latitude = 46.5950,
            Longitude = -112.4440,
            State = "MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "rogers-pass-mt",
            Name = "Rogers Pass",
            Highway = "MT-200",
            ElevationFeet = 5610,
            Latitude = 47.0392,
            Longitude = -112.5256,
            State = "MT",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "chief-joseph-mt",
            Name = "Chief Joseph Pass",
            Highway = "US-93",
            ElevationFeet = 7241,
            Latitude = 45.7269,
            Longitude = -113.8699,
            State = "MT/ID",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "homestake",
            Name = "Homestake Pass",
            Highway = "I-90",
            ElevationFeet = 6375,
            Latitude = 45.9099,
            Longitude = -112.7756,
            State = "MT",
            HasOfficialConditions = false
        }
    ];

    public static PassInfo? GetById(string id) =>
        Passes.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

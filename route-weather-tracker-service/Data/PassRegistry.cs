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
        },

        // ── Oregon — NWS weather; full ODOT TripCheck integration pending ────────
        new PassInfo
        {
            Id = "santiam",
            Name = "Santiam Pass",
            Highway = "US-20",
            ElevationFeet = 4817,
            Latitude = 44.4194,
            Longitude = -121.9946,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },
        new PassInfo
        {
            Id = "willamette",
            Name = "Willamette Pass",
            Highway = "US-58",
            ElevationFeet = 5128,
            Latitude = 43.5953,
            Longitude = -122.0540,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },
        new PassInfo
        {
            Id = "siskiyou",
            Name = "Siskiyou Summit",
            Highway = "I-5",
            ElevationFeet = 4310,
            Latitude = 42.0591,
            Longitude = -122.5353,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },
        new PassInfo
        {
            Id = "deadman",
            Name = "Deadman Pass",
            Highway = "I-84",
            ElevationFeet = 4193,
            Latitude = 45.6952,
            Longitude = -118.6489,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },
        new PassInfo
        {
            Id = "mckenzie",
            Name = "McKenzie Pass",
            Highway = "OR-242",
            ElevationFeet = 5325,
            Latitude = 44.2600,
            Longitude = -121.8697,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },
        new PassInfo
        {
            Id = "government-camp",
            Name = "Mt. Hood Corridor",
            Highway = "US-26",
            ElevationFeet = 3670,
            Latitude = 45.2939,
            Longitude = -121.7467,
            State = "OR",
            HasOfficialConditions = false,
            OfficialUrl = "https://tripcheck.com/Pages/RoadConditions"
        },

        // ── Colorado — NWS weather; CDOT COtrip integration pending (needs CDOT-ApiKey) ──
        new PassInfo
        {
            Id = "vail-pass",
            Name = "Vail Pass",
            Highway = "I-70",
            ElevationFeet = 10662,
            Latitude = 39.5449,
            Longitude = -106.2085,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "eisenhower-tunnel",
            Name = "Eisenhower/Johnson Tunnel",
            Highway = "I-70",
            ElevationFeet = 11013,
            Latitude = 39.6846,
            Longitude = -105.9066,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "loveland-pass",
            Name = "Loveland Pass",
            Highway = "US-6",
            ElevationFeet = 11990,
            Latitude = 39.6707,
            Longitude = -105.8893,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "berthoud-pass",
            Name = "Berthoud Pass",
            Highway = "US-40",
            ElevationFeet = 11307,
            Latitude = 39.7988,
            Longitude = -105.7754,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "rabbit-ears",
            Name = "Rabbit Ears Pass",
            Highway = "US-40",
            ElevationFeet = 9426,
            Latitude = 40.3818,
            Longitude = -106.5839,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "monarch-pass",
            Name = "Monarch Pass",
            Highway = "US-50",
            ElevationFeet = 11312,
            Latitude = 38.4940,
            Longitude = -106.3297,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "wolf-creek",
            Name = "Wolf Creek Pass",
            Highway = "US-160",
            ElevationFeet = 10857,
            Latitude = 37.4774,
            Longitude = -106.7980,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "red-mountain",
            Name = "Red Mountain Pass",
            Highway = "US-550",
            ElevationFeet = 11018,
            Latitude = 37.9057,
            Longitude = -107.7214,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "la-veta",
            Name = "La Veta Pass",
            Highway = "US-160",
            ElevationFeet = 9413,
            Latitude = 37.4946,
            Longitude = -105.0055,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "cameron-pass",
            Name = "Cameron Pass",
            Highway = "CO-14",
            ElevationFeet = 10276,
            Latitude = 40.5569,
            Longitude = -105.8884,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "hoosier-pass",
            Name = "Hoosier Pass",
            Highway = "CO-9",
            ElevationFeet = 11541,
            Latitude = 39.3580,
            Longitude = -106.0559,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "kenosha-pass",
            Name = "Kenosha Pass",
            Highway = "US-285",
            ElevationFeet = 9999,
            Latitude = 39.4198,
            Longitude = -105.7665,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "fremont-pass",
            Name = "Fremont Pass",
            Highway = "CO-91",
            ElevationFeet = 11318,
            Latitude = 39.3780,
            Longitude = -106.2075,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },
        new PassInfo
        {
            Id = "poncha-pass",
            Name = "Poncha Pass",
            Highway = "US-285",
            ElevationFeet = 9010,
            Latitude = 38.3536,
            Longitude = -106.0897,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        }
    ];

    public static PassInfo? GetById(string id) =>
        Passes.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

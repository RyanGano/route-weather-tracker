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
            Latitude = 48.5226,
            Longitude = -120.6553,
            State = "WA",
            OfficialUrl = "https://wsdot.com/travel/real-time/mountainpasses/washington"
        },
        new PassInfo
        {
            Id = "rainy-pass",
            Name = "Rainy Pass",
            Highway = "WA-20",
            ElevationFeet = 4855,
            Latitude = 48.5158,
            Longitude = -120.7365,
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
            Latitude = 47.6212,
            Longitude = -116.5230,
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
            Latitude = 44.3060,
            Longitude = -115.2317,
            State = "ID",
            HasOfficialConditions = false
        },
        new PassInfo
        {
            Id = "galena-summit",
            Name = "Galena Summit",
            Highway = "ID-75",
            ElevationFeet = 8701,
            Latitude = 43.8704,
            Longitude = -114.7128,
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
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
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
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
        },
        new PassInfo
        {
            Id = "macdonald",
            Name = "MacDonald Pass",
            Highway = "US-12",
            ElevationFeet = 6325,
            Latitude = 46.5615,
            Longitude = -112.3089,
            State = "MT",
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
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
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
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
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
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
            HasOfficialConditions = false,
            OfficialUrl = "https://travinfo.mdt.mt.gov/"
        },

        // ── Oregon — NWS weather; full ODOT TripCheck integration pending ────────
        new PassInfo
        {
            Id = "santiam",
            Name = "Santiam Pass",
            Highway = "US-20",
            ElevationFeet = 4817,
            Latitude = 44.4248,
            Longitude = -121.8455,
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
            ElevationFeet = 3640,
            Latitude = 45.6005,
            Longitude = -118.5040,
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
            Latitude = 45.3035,
            Longitude = -121.7548,
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
            Latitude = 39.6789,
            Longitude = -105.9348,
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
            Latitude = 40.3841,
            Longitude = -106.6118,
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
            Latitude = 37.8989,
            Longitude = -107.7119,
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
            Latitude = 37.6134,
            Longitude = -105.1894,
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
            Latitude = 39.3675,
            Longitude = -106.1876,
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
            Latitude = 38.4222,
            Longitude = -106.0881,
            State = "CO",
            HasOfficialConditions = false,
            OfficialUrl = "https://cotrip.org"
        },

        // ── California — NWS weather; Caltrans chain-control integration pending ─
        new PassInfo
        {
            Id = "donner",
            Name = "Donner Pass",
            Highway = "I-80",
            ElevationFeet = 7227,
            Latitude = 39.3224,
            Longitude = -120.3287,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "echo-summit",
            Name = "Echo Summit",
            Highway = "US-50",
            ElevationFeet = 7382,
            Latitude = 38.8108,
            Longitude = -120.0348,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "cajon",
            Name = "Cajon Pass",
            Highway = "I-15",
            ElevationFeet = 3777,
            Latitude = 34.3253,
            Longitude = -117.4287,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "tehachapi",
            Name = "Tehachapi Pass",
            Highway = "CA-58",
            ElevationFeet = 3793,
            Latitude = 35.1308,
            Longitude = -118.4383,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "monitor",
            Name = "Monitor Pass",
            Highway = "CA-89",
            ElevationFeet = 8314,
            Latitude = 38.6783,
            Longitude = -119.5985,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "tioga",
            Name = "Tioga Pass",
            Highway = "CA-120",
            ElevationFeet = 9943,
            Latitude = 37.9099,
            Longitude = -119.2552,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "sonora",
            Name = "Sonora Pass",
            Highway = "CA-108",
            ElevationFeet = 9624,
            Latitude = 38.3294,
            Longitude = -119.6264,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },
        new PassInfo
        {
            Id = "mt-shasta",
            Name = "Mt. Shasta Summit",
            Highway = "I-5",
            ElevationFeet = 3540,
            Latitude = 41.4028,
            Longitude = -122.3455,
            State = "CA",
            HasOfficialConditions = false,
            OfficialUrl = "https://roads.dot.ca.gov/"
        },

        // ── Wyoming — NWS weather; WYDOT ArcGIS integration pending ──────────────
        new PassInfo
        {
            Id = "teton-pass",
            Name = "Teton Pass",
            Highway = "WY-22",
            ElevationFeet = 8431,
            Latitude = 43.4952,
            Longitude = -110.9455,
            State = "WY",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },
        new PassInfo
        {
            Id = "togwotee",
            Name = "Togwotee Pass",
            Highway = "US-287",
            ElevationFeet = 9658,
            Latitude = 43.7601,
            Longitude = -110.0824,
            State = "WY",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },
        new PassInfo
        {
            Id = "snowy-range",
            Name = "Snowy Range Pass",
            Highway = "WY-130",
            ElevationFeet = 10847,
            Latitude = 41.3424,
            Longitude = -106.2997,
            State = "WY",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },
        new PassInfo
        {
            Id = "south-pass",
            Name = "South Pass",
            Highway = "WY-28",
            ElevationFeet = 7412,
            Latitude = 42.3647,
            Longitude = -108.5905,
            State = "WY",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },
        new PassInfo
        {
            Id = "powder-river",
            Name = "Powder River Pass",
            Highway = "US-16",
            ElevationFeet = 9666,
            Latitude = 44.1497,
            Longitude = -107.0795,
            State = "WY",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },
        new PassInfo
        {
            Id = "beartooth",
            Name = "Beartooth Pass",
            Highway = "US-212",
            ElevationFeet = 10947,
            Latitude = 44.9690,
            Longitude = -109.4713,
            State = "WY/MT",
            HasOfficialConditions = false,
            OfficialUrl = "https://wyoroad.info"
        },

        // ── Utah — NWS weather; UDOT ArcGIS integration pending ───────────────────
        new PassInfo
        {
            Id = "parleys",
            Name = "Parleys Canyon Summit",
            Highway = "I-80",
            ElevationFeet = 6755,
            Latitude = 40.6897,
            Longitude = -111.7437,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.udottraffic.utah.gov/map/#camera-90759"
        },
        new PassInfo
        {
            Id = "soldier-summit",
            Name = "Soldier Summit",
            Highway = "US-6",
            ElevationFeet = 7477,
            Latitude = 39.8358,
            Longitude = -110.9294,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.udottraffic.utah.gov/map/#camera-91505"
        },
        new PassInfo
        {
            Id = "sardine",
            Name = "Sardine Canyon",
            Highway = "US-91",
            ElevationFeet = 5540,
            Latitude = 41.6347,
            Longitude = -111.8491,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.udottraffic.utah.gov/map/#camera-91326"
        },
        new PassInfo
        {
            Id = "cedar-mountain",
            Name = "Cedar Canyon",
            Highway = "UT-14",
            ElevationFeet = 9240,
            Latitude = 37.6333,
            Longitude = -112.8424,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.udottraffic.utah.gov/map/#camera-91099"
        },
        new PassInfo
        {
            Id = "beaver-canyon",
            Name = "UT-153 Summit",
            Highway = "UT-153",
            ElevationFeet = 9400,
            Latitude = 38.3980,
            Longitude = -112.4614,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://udottraffic.utah.gov"
        },
        new PassInfo
        {
            Id = "pine-valley",
            Name = "Veyo / Black Ridge",
            Highway = "I-15",
            ElevationFeet = 5545,
            Latitude = 37.2650,
            Longitude = -113.6968,
            State = "UT",
            HasOfficialConditions = false,
            OfficialUrl = "https://udottraffic.utah.gov"
        },

        // ── Nevada — NWS weather; NDOT ArcGIS integration pending ─────────────────
        new PassInfo
        {
            Id = "spooner",
            Name = "Spooner Summit",
            Highway = "US-50",
            ElevationFeet = 7146,
            Latitude = 39.1013,
            Longitude = -119.9124,
            State = "NV",
            HasOfficialConditions = false,
            OfficialUrl = "https://nvroads.com"
        },
        new PassInfo
        {
            Id = "mount-rose",
            Name = "Mt. Rose Summit",
            Highway = "NV-431",
            ElevationFeet = 8911,
            Latitude = 39.3235,
            Longitude = -119.9140,
            State = "NV",
            HasOfficialConditions = false,
            OfficialUrl = "https://nvroads.com"
        },
        new PassInfo
        {
            Id = "golconda",
            Name = "Golconda Summit",
            Highway = "I-80",
            ElevationFeet = 5145,
            Latitude = 40.9209,
            Longitude = -117.3923,
            State = "NV",
            HasOfficialConditions = false,
            OfficialUrl = "https://nvroads.com"
        },
        new PassInfo
        {
            Id = "palisade",
            Name = "Palisade Canyon",
            Highway = "I-80",
            ElevationFeet = 4540,
            Latitude = 40.6102,
            Longitude = -116.1987,
            State = "NV",
            HasOfficialConditions = false,
            OfficialUrl = "https://nvroads.com"
        },

        // ── New Mexico — NWS weather; NMDOT ArcGIS integration pending ────────────
        new PassInfo
        {
            Id = "glorieta",
            Name = "Glorieta Pass",
            Highway = "I-25",
            ElevationFeet = 7432,
            Latitude = 35.5218,
            Longitude = -105.7700,
            State = "NM",
            HasOfficialConditions = false,
            OfficialUrl = "https://nmroads.com"
        },
        new PassInfo
        {
            Id = "tijeras",
            Name = "Tijeras Canyon",
            Highway = "I-40",
            ElevationFeet = 6534,
            Latitude = 35.0698,
            Longitude = -106.3855,
            State = "NM",
            HasOfficialConditions = false,
            OfficialUrl = "https://nmroads.com"
        },
        new PassInfo
        {
            Id = "raton",
            Name = "Raton Pass",
            Highway = "I-25",
            ElevationFeet = 7834,
            Latitude = 36.9977,
            Longitude = -104.5076,
            State = "NM",
            HasOfficialConditions = false,
            OfficialUrl = "https://nmroads.com"
        },
        new PassInfo
        {
            Id = "apache-summit",
            Name = "Apache Summit",
            Highway = "US-60",
            ElevationFeet = 7600,
            Latitude = 34.1350,
            Longitude = -109.8490,
            State = "NM",
            HasOfficialConditions = false,
            OfficialUrl = "https://nmroads.com"
        },
        new PassInfo
        {
            Id = "emory",
            Name = "Emory Pass",
            Highway = "NM-152",
            ElevationFeet = 8228,
            Latitude = 32.9243,
            Longitude = -107.7889,
            State = "NM",
            HasOfficialConditions = false,
            OfficialUrl = "https://nmroads.com"
        },

        // ── Virginia — NWS weather; VDOT 511 integration pending (needs VDOT-ApiKey)
        new PassInfo
        {
            Id = "afton-mountain",
            Name = "Afton Mountain",
            Highway = "I-64",
            ElevationFeet = 1929,
            Latitude = 37.9990,
            Longitude = -78.8980,
            State = "VA",
            HasOfficialConditions = false,
            OfficialUrl = "https://511virginia.org"
        },
        new PassInfo
        {
            Id = "rockfish-gap",
            Name = "Rockfish Gap",
            Highway = "US-250",
            ElevationFeet = 1909,
            Latitude = 38.0343,
            Longitude = -78.8682,
            State = "VA",
            HasOfficialConditions = false,
            OfficialUrl = "https://511virginia.org"
        },
        new PassInfo
        {
            Id = "fancy-gap",
            Name = "Fancy Gap",
            Highway = "I-77",
            ElevationFeet = 2895,
            Latitude = 36.6623,
            Longitude = -80.6460,
            State = "VA",
            HasOfficialConditions = false,
            OfficialUrl = "https://511virginia.org"
        },
        new PassInfo
        {
            Id = "shenandoah-gap",
            Name = "Front Royal (Skyline Dr)",
            Highway = "US-340",
            ElevationFeet = 1900,
            Latitude = 38.8717,
            Longitude = -78.2047,
            State = "VA",
            HasOfficialConditions = false,
            OfficialUrl = "https://511virginia.org"
        },

        // ── North Carolina / Tennessee — NWS weather; TDOT key pending ────────────
        new PassInfo
        {
            Id = "newfound-gap",
            Name = "Newfound Gap",
            Highway = "US-441",
            ElevationFeet = 5046,
            Latitude = 35.6112,
            Longitude = -83.4265,
            State = "NC/TN",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.nps.gov/grsm/planyourvisit/temproadclose.htm"
        },
        new PassInfo
        {
            Id = "cherokee",
            Name = "Big Witch Gap",
            Highway = "US-19",
            ElevationFeet = 3200,
            Latitude = 35.4450,
            Longitude = -83.2680,
            State = "NC",
            HasOfficialConditions = false,
            OfficialUrl = "https://drivenc.gov"
        },
        new PassInfo
        {
            Id = "clinch-mountain",
            Name = "Clinch Mountain",
            Highway = "US-11W",
            ElevationFeet = 3074,
            Latitude = 36.4311,
            Longitude = -83.1709,
            State = "TN",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.tn511.com"
        },
        new PassInfo
        {
            Id = "santeelah",
            Name = "Winding Stair Gap",
            Highway = "US-64",
            ElevationFeet = 3770,
            Latitude = 35.1199,
            Longitude = -83.5483,
            State = "NC",
            HasOfficialConditions = false,
            OfficialUrl = "https://drivenc.gov"
        },
        new PassInfo
        {
            Id = "clingmans-dome",
            Name = "Clingmans Dome Rd",
            Highway = "Spur",
            ElevationFeet = 6643,
            Latitude = 35.5629,
            Longitude = -83.4985,
            State = "NC/TN",
            HasOfficialConditions = false,
            OfficialUrl = "https://www.nps.gov/grsm/planyourvisit/temproadclose.htm"
        }
    ];

    public static PassInfo? GetById(string id) =>
        Passes.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

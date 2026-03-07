using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Data;

/// <summary>
/// Major US cities that can serve as route endpoints, grouped by state/region.
/// Coordinates are city-centre approximations (lat/lon decimal degrees WGS-84).
/// </summary>
public static class RouteEndpointRegistry
{
    public static readonly IReadOnlyList<Models.RouteEndpoint> Endpoints =
    [
        // ── Washington ──────────────────────────────────────────────────────────
        new() { Id = "bellingham",    Name = "Bellingham",    State = "WA", Latitude = 48.749, Longitude = -122.480 },
        new() { Id = "everett",       Name = "Everett",       State = "WA", Latitude = 47.980, Longitude = -122.202 },
        new() { Id = "stanwood",      Name = "Stanwood",      State = "WA", Latitude = 48.239, Longitude = -122.370 },
        new() { Id = "seattle",       Name = "Seattle",       State = "WA", Latitude = 47.608, Longitude = -122.335 },
        new() { Id = "tacoma",        Name = "Tacoma",        State = "WA", Latitude = 47.252, Longitude = -122.444 },
        new() { Id = "olympia",       Name = "Olympia",       State = "WA", Latitude = 47.037, Longitude = -122.901 },
        new() { Id = "yakima",        Name = "Yakima",        State = "WA", Latitude = 46.600, Longitude = -120.506 },
        new() { Id = "tri-cities",    Name = "Tri-Cities",    State = "WA", Latitude = 46.212, Longitude = -119.136 },
        new() { Id = "spokane",       Name = "Spokane",       State = "WA", Latitude = 47.658, Longitude = -117.426 },

        // ── Oregon ──────────────────────────────────────────────────────────────
        new() { Id = "astoria",       Name = "Astoria",       State = "OR", Latitude = 46.187, Longitude = -123.831 },
        new() { Id = "portland",      Name = "Portland",      State = "OR", Latitude = 45.523, Longitude = -122.676 },
        new() { Id = "salem",         Name = "Salem",         State = "OR", Latitude = 44.942, Longitude = -123.035 },
        new() { Id = "eugene",        Name = "Eugene",        State = "OR", Latitude = 44.052, Longitude = -123.087 },
        new() { Id = "bend",          Name = "Bend",          State = "OR", Latitude = 44.058, Longitude = -121.315 },
        new() { Id = "pendleton",     Name = "Pendleton",     State = "OR", Latitude = 45.672, Longitude = -118.788 },
        new() { Id = "medford",       Name = "Medford",       State = "OR", Latitude = 42.326, Longitude = -122.876 },

        // ── Idaho ────────────────────────────────────────────────────────────────
        new() { Id = "coeur-d-alene", Name = "Coeur d'Alene", State = "ID", Latitude = 47.677, Longitude = -116.780 },
        new() { Id = "lewiston",      Name = "Lewiston",      State = "ID", Latitude = 46.415, Longitude = -117.017 },
        new() { Id = "boise",         Name = "Boise",         State = "ID", Latitude = 43.615, Longitude = -116.202 },
        new() { Id = "twin-falls",    Name = "Twin Falls",    State = "ID", Latitude = 42.563, Longitude = -114.461 },
        new() { Id = "pocatello",     Name = "Pocatello",     State = "ID", Latitude = 42.867, Longitude = -112.446 },
        new() { Id = "idaho-falls",   Name = "Idaho Falls",   State = "ID", Latitude = 43.492, Longitude = -112.034 },

        // ── Montana ──────────────────────────────────────────────────────────────
        // Whitefish and Kalispell are reached faster from the west via US-2/Sandpoint,
        // but the common road is I-90 east through the Idaho passes then north on
        // US-93 from St. Regis, MT. Declaring St. Regis as a routing hub causes the
        // service to offer both options so the user sees the Idaho-pass corridor with
        // an accurate distance.
        new() { Id = "whitefish",  Name = "Whitefish",  State = "MT", Latitude = 48.412, Longitude = -114.336, RoutingHubs = ["st-regis"] },
        new() { Id = "kalispell",  Name = "Kalispell",  State = "MT", Latitude = 48.196, Longitude = -114.313, RoutingHubs = ["st-regis"] },
        new() { Id = "missoula",   Name = "Missoula",   State = "MT", Latitude = 46.872, Longitude = -113.994 },
        new() { Id = "st-regis",   Name = "St. Regis",  State = "MT", Latitude = 47.297, Longitude = -115.101 },
        new() { Id = "great-falls",   Name = "Great Falls",   State = "MT", Latitude = 47.500, Longitude = -111.300 },
        new() { Id = "helena",        Name = "Helena",        State = "MT", Latitude = 46.596, Longitude = -112.027 },
        new() { Id = "butte",         Name = "Butte",         State = "MT", Latitude = 46.003, Longitude = -112.534 },
        new() { Id = "bozeman",       Name = "Bozeman",       State = "MT", Latitude = 45.680, Longitude = -111.042 },
        new() { Id = "billings",      Name = "Billings",      State = "MT", Latitude = 45.783, Longitude = -108.501 },

        // ── Wyoming ──────────────────────────────────────────────────────────────
        new() { Id = "cody",          Name = "Cody",          State = "WY", Latitude = 44.526, Longitude = -109.057 },
        new() { Id = "jackson",       Name = "Jackson",       State = "WY", Latitude = 43.480, Longitude = -110.762 },
        new() { Id = "rock-springs",  Name = "Rock Springs",  State = "WY", Latitude = 41.588, Longitude = -109.203 },
        new() { Id = "casper",        Name = "Casper",        State = "WY", Latitude = 42.867, Longitude = -106.313 },
        new() { Id = "laramie",       Name = "Laramie",       State = "WY", Latitude = 41.311, Longitude = -105.591 },
        new() { Id = "cheyenne",      Name = "Cheyenne",      State = "WY", Latitude = 41.140, Longitude = -104.820 },

        // ── Colorado ─────────────────────────────────────────────────────────────
        new() { Id = "steamboat-springs", Name = "Steamboat Springs", State = "CO", Latitude = 40.485, Longitude = -106.831 },
        new() { Id = "fort-collins",  Name = "Fort Collins",  State = "CO", Latitude = 40.585, Longitude = -105.084 },
        new() { Id = "glenwood-springs", Name = "Glenwood Springs", State = "CO", Latitude = 39.548, Longitude = -107.325 },
        new() { Id = "grand-junction", Name = "Grand Junction", State = "CO", Latitude = 39.064, Longitude = -108.550 },
        new() { Id = "denver",        Name = "Denver",        State = "CO", Latitude = 39.739, Longitude = -104.984 },
        new() { Id = "colorado-springs", Name = "Colorado Springs", State = "CO", Latitude = 38.834, Longitude = -104.821 },
        new() { Id = "pueblo",        Name = "Pueblo",        State = "CO", Latitude = 38.254, Longitude = -104.609 },
        new() { Id = "durango",       Name = "Durango",       State = "CO", Latitude = 37.275, Longitude = -107.880 },

        // ── Utah ─────────────────────────────────────────────────────────────────
        new() { Id = "ogden",         Name = "Ogden",         State = "UT", Latitude = 41.223, Longitude = -111.974 },
        new() { Id = "salt-lake-city", Name = "Salt Lake City", State = "UT", Latitude = 40.761, Longitude = -111.891 },
        new() { Id = "provo",         Name = "Provo",         State = "UT", Latitude = 40.234, Longitude = -111.659 },
        new() { Id = "price",         Name = "Price",         State = "UT", Latitude = 39.599, Longitude = -110.811 },
        new() { Id = "moab",          Name = "Moab",          State = "UT", Latitude = 38.573, Longitude = -109.550 },
        new() { Id = "cedar-city",    Name = "Cedar City",    State = "UT", Latitude = 37.677, Longitude = -113.061 },
        new() { Id = "st-george",     Name = "St. George",    State = "UT", Latitude = 37.104, Longitude = -113.583 },

        // ── Nevada ───────────────────────────────────────────────────────────────
        new() { Id = "winnemucca",    Name = "Winnemucca",    State = "NV", Latitude = 40.973, Longitude = -117.736 },
        new() { Id = "elko",          Name = "Elko",          State = "NV", Latitude = 40.832, Longitude = -115.763 },
        new() { Id = "reno",          Name = "Reno",          State = "NV", Latitude = 39.529, Longitude = -119.813 },
        new() { Id = "las-vegas",     Name = "Las Vegas",     State = "NV", Latitude = 36.175, Longitude = -115.136 },

        // ── California ───────────────────────────────────────────────────────────
        new() { Id = "redding",       Name = "Redding",       State = "CA", Latitude = 40.587, Longitude = -122.392 },
        new() { Id = "sacramento",    Name = "Sacramento",    State = "CA", Latitude = 38.575, Longitude = -121.479 },
        new() { Id = "san-francisco", Name = "San Francisco", State = "CA", Latitude = 37.774, Longitude = -122.419 },
        new() { Id = "fresno",        Name = "Fresno",        State = "CA", Latitude = 36.737, Longitude = -119.787 },
        new() { Id = "bishop",        Name = "Bishop",        State = "CA", Latitude = 37.363, Longitude = -118.395 },
        new() { Id = "bakersfield",   Name = "Bakersfield",   State = "CA", Latitude = 35.373, Longitude = -119.019 },
        new() { Id = "los-angeles",   Name = "Los Angeles",   State = "CA", Latitude = 34.052, Longitude = -118.244 },
        new() { Id = "san-diego",     Name = "San Diego",     State = "CA", Latitude = 32.715, Longitude = -117.157 },

        // ── Arizona ──────────────────────────────────────────────────────────────
        new() { Id = "flagstaff",     Name = "Flagstaff",     State = "AZ", Latitude = 35.198, Longitude = -111.651 },
        new() { Id = "prescott",      Name = "Prescott",      State = "AZ", Latitude = 34.540, Longitude = -112.469 },
        new() { Id = "phoenix",       Name = "Phoenix",       State = "AZ", Latitude = 33.449, Longitude = -112.074 },
        new() { Id = "tucson",        Name = "Tucson",        State = "AZ", Latitude = 32.222, Longitude = -110.926 },

        // ── New Mexico ───────────────────────────────────────────────────────────
        new() { Id = "santa-fe",      Name = "Santa Fe",      State = "NM", Latitude = 35.687, Longitude = -105.938 },
        new() { Id = "albuquerque",   Name = "Albuquerque",   State = "NM", Latitude = 35.085, Longitude = -106.651 },
        new() { Id = "las-cruces",    Name = "Las Cruces",    State = "NM", Latitude = 32.312, Longitude = -106.778 },

        // ── Texas ────────────────────────────────────────────────────────────────
        new() { Id = "amarillo",      Name = "Amarillo",      State = "TX", Latitude = 35.221, Longitude = -101.831 },
        new() { Id = "el-paso",       Name = "El Paso",       State = "TX", Latitude = 31.758, Longitude = -106.487 },
        new() { Id = "dallas",        Name = "Dallas",        State = "TX", Latitude = 32.776, Longitude =  -96.797 },

        // ── Midwest ──────────────────────────────────────────────────────────────
        new() { Id = "sioux-falls",   Name = "Sioux Falls",   State = "SD", Latitude = 43.550, Longitude =  -96.700 },
        new() { Id = "omaha",         Name = "Omaha",         State = "NE", Latitude = 41.258, Longitude =  -95.934 },
        new() { Id = "minneapolis",   Name = "Minneapolis",   State = "MN", Latitude = 44.977, Longitude =  -93.265 },
        new() { Id = "kansas-city",   Name = "Kansas City",   State = "MO", Latitude = 39.099, Longitude =  -94.578 },
        new() { Id = "chicago",       Name = "Chicago",       State = "IL", Latitude = 41.878, Longitude =  -87.629 },
        new() { Id = "st-louis",      Name = "St. Louis",     State = "MO", Latitude = 38.627, Longitude =  -90.199 },
        new() { Id = "indianapolis",  Name = "Indianapolis",  State = "IN", Latitude = 39.768, Longitude =  -86.158 },
        new() { Id = "columbus",      Name = "Columbus",      State = "OH", Latitude = 39.961, Longitude =  -82.999 },
        new() { Id = "cleveland",     Name = "Cleveland",     State = "OH", Latitude = 41.499, Longitude =  -81.694 },
        new() { Id = "cincinnati",    Name = "Cincinnati",    State = "OH", Latitude = 39.103, Longitude =  -84.512 },
        new() { Id = "detroit",       Name = "Detroit",       State = "MI", Latitude = 42.331, Longitude =  -83.046 },

        // ── Virginia and Appalachian states ──────────────────────────────────────
        new() { Id = "richmond",        Name = "Richmond",        State = "VA", Latitude = 37.541,  Longitude =  -77.436 },
        new() { Id = "roanoke",         Name = "Roanoke",         State = "VA", Latitude = 37.271,  Longitude =  -79.941 },
        new() { Id = "charlottesville", Name = "Charlottesville", State = "VA", Latitude = 38.029,  Longitude =  -78.477 },
        new() { Id = "washington-dc",   Name = "Washington DC",   State = "DC", Latitude = 38.907,  Longitude =  -77.037 },
        new() { Id = "baltimore",       Name = "Baltimore",       State = "MD", Latitude = 39.290,  Longitude =  -76.612 },
        new() { Id = "pittsburgh",      Name = "Pittsburgh",      State = "PA", Latitude = 40.441,  Longitude =  -79.996 },
        new() { Id = "philadelphia",    Name = "Philadelphia",    State = "PA", Latitude = 39.953,  Longitude =  -75.165 },
        new() { Id = "new-york",        Name = "New York",        State = "NY", Latitude = 40.713,  Longitude =  -74.006 },
        new() { Id = "boston",          Name = "Boston",          State = "MA", Latitude = 42.360,  Longitude =  -71.059 },
        new() { Id = "norfolk",         Name = "Norfolk",         State = "VA", Latitude = 36.851,  Longitude =  -76.286 },
        new() { Id = "lexington-ky",    Name = "Lexington",       State = "KY", Latitude = 38.041,  Longitude =  -84.504 },
        new() { Id = "louisville",      Name = "Louisville",      State = "KY", Latitude = 38.253,  Longitude =  -85.759 },

        // ── North Carolina / Tennessee / Southeast ────────────────────────────────
        new() { Id = "charlotte-nc",    Name = "Charlotte",       State = "NC", Latitude = 35.227,  Longitude =  -80.843 },
        new() { Id = "asheville",       Name = "Asheville",       State = "NC", Latitude = 35.595,  Longitude =  -82.552 },
        new() { Id = "knoxville",       Name = "Knoxville",       State = "TN", Latitude = 35.961,  Longitude =  -83.921 },
        new() { Id = "chattanooga",     Name = "Chattanooga",     State = "TN", Latitude = 35.046,  Longitude =  -85.310 },
        new() { Id = "nashville",       Name = "Nashville",       State = "TN", Latitude = 36.163,  Longitude =  -86.782 },
        new() { Id = "memphis",         Name = "Memphis",         State = "TN", Latitude = 35.150,  Longitude =  -90.049 },
        new() { Id = "atlanta",         Name = "Atlanta",         State = "GA", Latitude = 33.749,  Longitude =  -84.388 },
        new() { Id = "greenville-sc",   Name = "Greenville",      State = "SC", Latitude = 34.853,  Longitude =  -82.394 },
        new() { Id = "birmingham",      Name = "Birmingham",      State = "AL", Latitude = 33.519,  Longitude =  -86.810 },
        new() { Id = "jacksonville",    Name = "Jacksonville",    State = "FL", Latitude = 30.332,  Longitude =  -81.656 },
        new() { Id = "miami",           Name = "Miami",           State = "FL", Latitude = 25.762,  Longitude =  -80.192 },
        new() { Id = "orlando",         Name = "Orlando",         State = "FL", Latitude = 28.538,  Longitude =  -81.379 },
        new() { Id = "tampa",           Name = "Tampa",           State = "FL", Latitude = 27.951,  Longitude =  -82.457 },
        new() { Id = "new-orleans",     Name = "New Orleans",     State = "LA", Latitude = 29.951,  Longitude =  -90.072 },
        new() { Id = "oklahoma-city",   Name = "Oklahoma City",   State = "OK", Latitude = 35.468,  Longitude =  -97.516 },
        new() { Id = "wichita",         Name = "Wichita",         State = "KS", Latitude = 37.687,  Longitude =  -97.330 },
    ];

    public static Models.RouteEndpoint? GetById(string id) =>
        Endpoints.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}

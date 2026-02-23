# State-by-State Implementation Plan

This document is the authoritative guide for adding mountain pass and routing coverage
to the Route Weather Tracker. Each state section describes the public data sources,
known passes, and implementation checklist. Work through states in priority order;
each completed state should be committed as its own git commit before moving on.

---

## Architecture Overview ✅

> **Implemented** (`4331bc4`): `IRoutingService` / `OsrmRoutingService`, `IPassLocatorService` / `PassLocatorService`,
> `ComputedRoute` model, `/api/routes/compute` endpoint, `PassAggregatorService` null-source fix,
> `PassInfo.HasOfficialConditions`, frontend `computeRoutes()` + `getPassesByIds()` helpers.

### How a city-to-city route is resolved

```
User picks Origin + Destination  (from RouteEndpointRegistry)
            │
            ▼
IRoutingService.GetRoutesAsync(origin, destination)
            │
            ├─ Calls OSRM: GET https://router.project-osrm.org/route/v1/driving
            │   {originLon},{originLat};{destLon},{destLat}
            │   ?alternatives=true&steps=true&geometries=geojson&overview=full
            │
            └─ Returns List<ComputedRoute>
                  • HighwaysUsed  (from step "ref" tags: "I-90", "US-2", …)
                  • RouteGeometry (GeoJSON LineString)
                  • DistanceMiles / EstimatedMinutes
                  │
                  ▼
IPassLocatorService.FindPassesOnRoute(route)
            │
            ├─ Iterates PassRegistry.Passes
            └─ Includes pass if its lat/lon is within ~15 km of the route line
                  │
                  ▼
IPassAggregatorService.GetPassesAsync(matchedPassIds)
            │
            └─ Dispatches to per-state IPassDataSource implementations
               (WSDOT, IdahoTransport, ODOT, CDOT, …) + OpenWeatherService fallback
```

### Key interfaces to add/extend

| Interface             | File                              | Purpose                              |
| --------------------- | --------------------------------- | ------------------------------------ |
| `IRoutingService`     | `Services/IRoutingService.cs`     | Compute city-to-city routes via OSRM |
| `OsrmRoutingService`  | `Services/OsrmRoutingService.cs`  | OSRM HTTP client implementation      |
| `IPassLocatorService` | `Services/IPassLocatorService.cs` | Geometric pass-on-route matching     |
| `PassLocatorService`  | `Services/PassLocatorService.cs`  | Haversine distance to route polyline |
| `ComputedRoute`       | `Models/ComputedRoute.cs`         | Route result returned to the client  |

### OSRM public demo server

- Base URL: `https://router.project-osrm.org`
- No API key required. Rate limit: ~10 req/s, fair-use.
- For self-hosting: https://github.com/Project-OSRM/osrm-backend
- Response of interest: `routes[].legs[].steps[].ref` (highway number tag)

### Geocoding (city name → coordinates)

- Nominatim: `https://nominatim.openstreetmap.org/search?q={city},{state}&format=json&limit=1`
- No key required. Required header: `User-Agent: route-weather-tracker/1.0`.
- Cache results; do not hammer for the same city twice.

### OpenWeather fallback

Already integrated (`IOpenWeatherService`). For any pass whose state lacks a condition
API, weather data is the fallback for "road status". Mark `PassInfo.HasOfficialConditions = false`
so the UI can indicate data source quality.

---

## Status Legend

| Symbol | Meaning                |
| ------ | ---------------------- |
| ✅     | Complete and committed |
| 🔄     | In progress            |
| ⬜     | Not started            |

---

## Implementation Checklist (per state)

For each state:

- [ ] Add `PassInfo` entries to `PassRegistry.cs`
- [ ] Add endpoint cities to `RouteEndpointRegistry.cs`
- [ ] Implement `IPassDataSource` (or confirm OpenWeather-only fallback)
- [ ] Register the new `IPassDataSource` in `Program.cs`
- [ ] Add condition + camera URL patterns to the data source
- [ ] Write unit tests in `route-weather-tracker-service.Tests`
- [ ] `git add -A && git commit -m "feat(<state>): add <state> passes and data source"`

---

## Priority Order

1. **Oregon** — well-documented ODOT/TripCheck API, major I-5 and US-20 corridors
2. **Colorado** — best-documented state API (COtrip), most passes of any state
3. ~~**Montana**~~ ✅ scaffold committed — MDT 511 integration still pending
4. **California** — Donner Pass (I-80) is highest-traffic winter pass in the US
5. **Wyoming** — Teton Pass, iconic gateway corridor
6. **Utah** — I-80 and I-15 western arterials
7. **Nevada** — US-50 and I-80 connectors
8. **New Mexico** — I-25 and I-40 southern routes
9. **Virginia** — I-64 / I-77 eastern mountain corridors
10. **North Carolina / Tennessee** — Blue Ridge / Smokies passes

---

## Existing States (Reference)

### Washington (WA) ✅

**Data source:** WSDOT — `IWsdotService` / `WsdotPassDataSource`
**API:** `https://www.wsdot.wa.gov/Traffic/api/MountainPassConditions/MountainPassConditionsREST.svc/GetMountainPassConditionsAsJSON?AccessCode={key}`
**Auth:** Free API key — https://wsdot.wa.gov/traffic/api/
**Data:** Road conditions (surface, traction, restrictions), webcam images, travel advisories
**Official site:** https://wsdot.com/travel/real-time/mountainpasses

#### Passes in PassRegistry (all ✅ committed)

| Pass            | Highway | Elevation (ft) | Lat     | Lon       | WSDOT ID |
| --------------- | ------- | -------------- | ------- | --------- | -------- |
| Snoqualmie Pass | I-90    | 3,022          | 47.4245 | -121.4116 | 11       |
| Stevens Pass    | US-2    | 4,061          | 47.7447 | -121.0891 | 2        |
| Cayuse Pass     | WA-123  | 4,694          | 46.8706 | -121.5445 | 1        |
| White Pass      | US-12   | 4,500          | 46.6388 | -121.3988 | 3        |
| Washington Pass | WA-20   | 5,477          | 48.5195 | -120.6653 | 4        |
| Rainy Pass      | WA-20   | 4,855          | 48.5195 | -120.7364 | n/a (OpenWeather-only) |
| Sherman Pass    | WA-20   | 5,575          | 48.6030 | -118.4630 | 5        |

---

### Idaho (ID) ✅

**Data source:** Idaho 511 — `IIdahoTransportService` / `IdahoPassDataSource`
**API:** Camera image scraping from `https://511.idaho.gov`
**Auth:** None required
**Data:** Camera images only (no official road conditions from Idaho DOT via API)
**Official site:** https://511.idaho.gov

#### Passes in PassRegistry (all ✅ committed)

| Pass                | Highway | Elevation (ft) | Lat     | Lon       | Source         |
| ------------------- | ------- | -------------- | ------- | --------- | -------------- |
| Fourth of July Pass | I-90    | 3,081          | 47.5333 | -116.3667 | Idaho 511 cams |
| Lookout Pass        | I-90    | 4,738          | 47.4576 | -115.6990 | Idaho 511 cams |
| Lolo Pass           | US-12   | 5,233          | 46.6494 | -114.5983 | OpenWeather-only |
| Lost Trail Pass     | US-93   | 6,995          | 45.6800 | -113.9500 | OpenWeather-only |
| Banner Summit       | ID-21   | 6,989          | 44.2608 | -114.9731 | OpenWeather-only |
| Galena Summit       | ID-75   | 8,701          | 43.8742 | -114.6978 | OpenWeather-only |

---

---

## Oregon (OR) ⬜

### Data Source

**Agency:** Oregon DOT (ODOT)
**Site:** https://tripcheck.com
**API base:** `https://api.tripcheck.com/api/` (no auth required for public read)

Key endpoints:

```
GET https://api.tripcheck.com/api/roadsegments
    ?routeName=US-20&county=&limit=50
    → Returns road condition segments with travelability codes

GET https://api.tripcheck.com/api/cameras
    ?routeName=US-20
    → Returns camera objects: { id, name, url (JPEG), lat, lon }

GET https://api.tripcheck.com/api/weatherstations
    → RWIS weather station data at mountain pass locations
```

**Auth:** None. Public JSON endpoints. Review TripCheck terms of service.
**Data available:** Road conditions (Open/Restricted/Closed), traction advisories,
chain requirements, camera images (JPEG URL, refreshed ~2 min), weather station data.

**Official condition report (human-readable):**
https://tripcheck.com/DynamicReports/Report/RoadConditions

**ODOT TripCheck API documentation:**
https://tripcheck.com/Pages/API

### Implementation Plan

Create `OdotService.cs` / `IOdotService.cs` and `OregonPassDataSource.cs`.
Pattern mirrors `WsdotService` — fetch road condition for the segment containing
the pass, map ODOT travelability codes → `PassCondition`.

ODOT travelability codes:
| Code | Meaning |
|---|---|
| 1 | Normal conditions |
| 2 | Wet |
| 3 | Snow or ice |
| 4 | Restricted |
| 5 | Closed |

```csharp
// Example: Santiam Pass condition
GET https://api.tripcheck.com/api/roadsegments?routeName=US-20&county=Linn
// Filter results to segment whose startMilepost <= 88 <= endMilepost (Santiam is MP ~88)
```

### Passes to Add

| Pass ID           | Name              | Highway | Elevation (ft) | Lat     | Lon       | Notes                        |
| ----------------- | ----------------- | ------- | -------------- | ------- | --------- | ---------------------------- |
| `santiam`         | Santiam Pass      | US-20   | 4,817          | 44.4194 | -121.9946 | Primary OR Cascades crossing |
| `willamette`      | Willamette Pass   | US-58   | 5,128          | 43.5953 | -122.0540 | Eugene to Klamath Falls      |
| `siskiyou`        | Siskiyou Summit   | I-5     | 4,310          | 42.0591 | -122.5353 | I-5 OR-CA border             |
| `deadman`         | Deadman Pass      | I-84    | 4,193          | 45.6952 | -118.6489 | I-84 Blue Mountains          |
| `mckenzie`        | McKenzie Pass     | OR-242  | 5,325          | 44.2600 | -121.8697 | Seasonal (Jun–Oct only)      |
| `government-camp` | Mt. Hood Corridor | US-26   | 3,670          | 45.2939 | -121.7467 | US-26 near Government Camp   |

### Cities to Add to RouteEndpointRegistry

| Id          | Name      | State | Lat     | Lon       |
| ----------- | --------- | ----- | ------- | --------- |
| `portland`  | Portland  | OR    | 45.5051 | -122.6750 |
| `eugene`    | Eugene    | OR    | 44.0521 | -123.0868 |
| `bend`      | Bend      | OR    | 44.0582 | -121.3153 |
| `medford`   | Medford   | OR    | 42.3265 | -122.8756 |
| `pendleton` | Pendleton | OR    | 45.6721 | -118.7886 |

### Commit message

```
feat(or): add Oregon passes and ODOT TripCheck data source
```

---

## Colorado (CO) ⬜

### Data Source

**Agency:** Colorado DOT (CDOT)
**Site:** https://cotrip.org
**API base:** `https://data.cotrip.org/api/v1/`
**Auth:** Free API key — register at https://manage-api.cotrip.org/login
(Google, Twitter, or email signup; key delivered immediately)
**Store key as:** `CDOT_API_KEY` in `appsettings.Development.json` / Azure Key Vault

Key endpoints:

```
GET https://data.cotrip.org/api/v1/roadConditions?apiKey={key}
    → All active road condition reports statewide (JSON array)
    Fields: id, roadway, direction, travel_center_point (lat/lon), condition_code, description, update_time

GET https://data.cotrip.org/api/v1/cameras?apiKey={key}
    → All cameras statewide
    Fields: id, name, location (lat/lon), views[].url

GET https://data.cotrip.org/api/v1/incidents?apiKey={key}
    → Active crashes and closures
```

Full data feed guide:
https://docs.google.com/document/d/e/2PACX-1vRS4iHQQ4KCTIwakn01G8LLopKfHn79OmZM77yEfl5yWX3C4k8KSvkstwaqobRdoLBXDkuZIyLqMLU4/pub

**CDOT condition_code values (road conditions feed):**
| Code | Meaning |
|---|---|
| `dry` | Dry |
| `wet` | Wet pavement |
| `snowPacked` | Packed snow |
| `icePatch` | Ice patches |
| `iceLayer` | Ice layer |
| `snowSlush` | Slush |
| `closed` | Road closed |

**Implementation note:** CDOT returns conditions by road _segment_, not by named pass.
Match a pass to the nearest `roadConditions` segment using the pass lat/lon and
the segment's `travel_center_point`. Cache the full list and find nearest per pass
to avoid calling per-pass.

### Implementation Plan

Create `CdotService.cs` / `ICdotService.cs` and `ColoradoPassDataSource.cs`.
Single bulk fetch → cache the statewide list → `GetConditionAsync` picks closest segment.

### Passes to Add

| Pass ID             | Name                      | Highway | Elevation (ft) | Lat     | Lon       | Notes                              |
| ------------------- | ------------------------- | ------- | -------------- | ------- | --------- | ---------------------------------- |
| `vail-pass`         | Vail Pass                 | I-70    | 10,662         | 39.5449 | -106.2085 | Most frequently closed CO pass     |
| `eisenhower-tunnel` | Eisenhower/Johnson Tunnel | I-70    | 11,013         | 39.6846 | -105.9066 | Actual tunnel, but modeled as pass |
| `loveland-pass`     | Loveland Pass             | US-6    | 11,990         | 39.6707 | -105.8893 | Alternate to Eisenhower Tunnel     |
| `berthoud-pass`     | Berthoud Pass             | US-40   | 11,307         | 39.7988 | -105.7754 | Denver ↔ Granby / Steamboat        |
| `rabbit-ears`       | Rabbit Ears Pass          | US-40   | 9,426          | 40.3818 | -106.5839 | Steamboat Springs approach         |
| `monarch-pass`      | Monarch Pass              | US-50   | 11,312         | 38.4940 | -106.3297 | Gunnison to Salida                 |
| `wolf-creek`        | Wolf Creek Pass           | US-160  | 10,857         | 37.4774 | -106.7980 | Southwest CO                       |
| `red-mountain`      | Red Mountain Pass         | US-550  | 11,018         | 37.9057 | -107.7214 | Million Dollar Highway             |
| `la-veta`           | La Veta Pass              | US-160  | 9,413          | 37.4946 | -105.0055 | Walsenburg to Alamosa              |
| `cameron-pass`      | Cameron Pass              | CO-14   | 10,276         | 40.5569 | -105.8884 | Fort Collins to Walden             |
| `hoosier-pass`      | Hoosier Pass              | CO-9    | 11,541         | 39.3580 | -106.0559 | Fairplay to Breckenridge           |
| `kenosha-pass`      | Kenosha Pass              | US-285  | 9,999          | 39.4198 | -105.7665 | Denver to South Park               |
| `fremont-pass`      | Fremont Pass              | CO-91   | 11,318         | 39.3780 | -106.2075 | Leadville area                     |
| `poncha-pass`       | Poncha Pass               | US-285  | 9,010          | 38.3536 | -106.0897 | Salida to South Fork               |

### Cities to Add to RouteEndpointRegistry

| Id                  | Name              | State | Lat     | Lon       |
| ------------------- | ----------------- | ----- | ------- | --------- |
| `denver`            | Denver            | CO    | 39.7392 | -104.9903 |
| `colorado-springs`  | Colorado Springs  | CO    | 38.8339 | -104.8214 |
| `grand-junction`    | Grand Junction    | CO    | 39.0639 | -108.5506 |
| `pueblo`            | Pueblo            | CO    | 38.2544 | -104.6091 |
| `durango`           | Durango           | CO    | 37.2753 | -107.8801 |
| `steamboat-springs` | Steamboat Springs | CO    | 40.4850 | -106.8317 |

### Commit message

```
feat(co): add Colorado passes and CDOT COtrip data source
```

---

## Montana (MT) ✅ (scaffold — OpenWeather-only; MDT integration pending)

> **Implemented** (`b47cc1d`): `MontanaPassDataSource` registered in DI. All 6 passes are in
> `PassRegistry` with `HasOfficialConditions = false`. Conditions come from OpenWeatherMap;
> cameras will be added when MDT 511 camera IDs are confirmed.
> To enable official conditions: implement `IMtdService` / `MtdService` (XML feed parser),
> inject into `MontanaPassDataSource`, and set `HasOfficialConditions = true`.

### Data Source

**Agency:** Montana DOT (MDT)
**Site:** https://www.mdt511.com / https://www.511mt.net
**API:** MDT participates in the 511 nationwide initiative. Condition data is available
via MDT's ArcGIS REST services and a road conditions XML feed.

```
GET https://www.mdt.mt.gov/travinfo/services/roads/currentconditions.aspx
    → XML road condition records (no key required)
    Fields: SegmentID, RoadName, Direction, Condition, TravelAdvisory

GET https://www.mdt.mt.gov/travinfo/services/cameras/camerainfo.aspx
    → XML camera inventory with JPG image URLs
```

**Alternative (more reliable):** MDT ArcGIS Feature Service

```
GET https://services.arcgis.com/qnjIrwR8z5Izc0ij/arcgis/rest/services/
    MontanaRoadConditions/FeatureServer/0/query
    ?where=1%3D1&outFields=*&f=json
```

**Auth:** None required for public services.
**Note:** If XML feed is unavailable or changes, OpenWeather serves as full fallback.
Check MDT developer portal at https://www.mdt.mt.gov/travinfo/ for updates.

### Implementation Plan

Create `MtdService.cs` / `IMtdService.cs` and `MontanaPassDataSource.cs`.
Parse the XML condition feed, match road names (e.g., "US-2", "US-12", "I-90") to the
known pass segments. Conditions are reported by road segment mile-post range.

### Passes in PassRegistry ✅

| Pass ID           | Name              | Highway                | Elevation (ft) | Lat     | Lon       | Notes                              |
| ----------------- | ----------------- | ---------------------- | -------------- | ------- | --------- | ---------------------------------- |
| `marias`          | Marias Pass       | US-2                   | 5,213          | 48.3139 | -112.9964 | Havre to Whitefish (US-2 corridor) |
| `logan-pass`      | Logan Pass        | US-89/Going-to-the-Sun | 6,646          | 48.6959 | -113.7181 | Glacier NP; seasonal (Jun–Oct)     |
| `chief-joseph-mt` | Chief Joseph Pass | US-93                  | 7,241          | 45.7269 | -113.8699 | MT/ID border on US-93              |
| `macdonald`       | MacDonald Pass    | US-12                  | 6,325          | 46.5950 | -112.4440 | Helena west approach               |
| `rogers-pass-mt`  | Rogers Pass       | US-12/MT-200           | 5,610          | 47.0392 | -112.5256 | Lincoln to Augusta                 |
| `homestake`       | Homestake Pass    | I-90                   | 6,375          | 45.9099 | -112.7756 | Butte bypass                       |

### Cities in RouteEndpointRegistry ✅

| Id            | Name        | State | Lat     | Lon       |
| ------------- | ----------- | ----- | ------- | --------- |
| `billings`    | Billings    | MT    | 45.7833 | -108.5007 |
| `great-falls` | Great Falls | MT    | 47.5002 | -111.3008 |
| `helena`      | Helena      | MT    | 46.5958 | -112.0270 |
| `bozeman`     | Bozeman     | MT    | 45.6770 | -111.0429 |
| `whitefish`   | Whitefish   | MT    | 48.4108 | -114.3361 |
| `butte`       | Butte       | MT    | 46.0038 | -112.5348 |

### Commit message

```
feat(mt): add Montana passes and MDT data source
```
> ✅ Committed as `feat(mt): add Montana passes and MontanaPassDataSource scaffold; fix RouteEndpoint ambiguity` (`b47cc1d`)

---

## California (CA) ⬜

### Data Source

**Agency:** California DOT (Caltrans)
**Site:** https://quickmap.dot.ca.gov / https://roads.dot.ca.gov
**Best API:** Caltrans publishes a public ArcGIS REST services endpoint and a
Statewide Integrated Traffic Records System (SWITRS) feed.

```
# Caltrans Road Conditions (511 statewide feed — RITIS/TMC format)
GET https://roads.dot.ca.gov/roadscond/feed/rss/district3
    → RSS feed, District 3 covers Sierra Nevada (I-80, US-50 Donner area)

# Caltrans QuickMap ArcGIS REST (most reliable for conditions + cameras)
GET https://cwwp2.dot.ca.gov/tools/cctv/cameras.json
    → All active cameras in the state; filter by district or coordinates

GET https://cwwp2.dot.ca.gov/tools/cciv/locations/cciv_locs.json
    → Camera inventory with lat/lon (filter to Sierra Nevada passes)
```

**Alternative — Caltrans open data portal:**
https://gisdata.dot.ca.gov/

**District 3** (Sacramento–Sierra Nevada): covers I-80, US-50, CA-89, CA-88
District numbers to know:

- District 3: I-80 (Donner), US-50 (Echo Summit), CA-89, CA-88
- District 7: I-15 (Cajon Pass), LA metro
- District 9: US-395 corridor, Monitor Pass

**Auth:** None for public feeds. No key required.

**Sierra Nevada Road/Pass Condition Hotline:** 1-800-427-7623
(Can also be automated via text-parsing of official closure pages if API becomes unavailable)

**Caltrans Chain Control:**
https://roads.dot.ca.gov/ — Chain control R1/R2/R3 data available as JSON feed:

```
GET https://www.dot.ca.gov/d3/chaincontrol/chaincontrol.json
```

This gives active chain control requirements by highway segment.

### Implementation Plan

Create `CaltransService.cs` / `ICaltransService.cs` and `CaliforniaPassDataSource.cs`.
Prioritize the CCTV camera JSON feed + chain control JSON — both are no-key public JSON.
For road conditions, parse the District 3 RSS feed for the I-80/US-50 corridor.
Map chain control codes:

- R1 = Chains or snow tires required
- R2 = Chains required on all vehicles
- R3 = Road closed

### Passes to Add

| Pass ID       | Name              | Highway | Elevation (ft) | Lat     | Lon       | Notes                           |
| ------------- | ----------------- | ------- | -------------- | ------- | --------- | ------------------------------- |
| `donner`      | Donner Pass       | I-80    | 7,227          | 39.3224 | -120.3287 | Highest-traffic Sierra crossing |
| `echo-summit` | Echo Summit       | US-50   | 7,382          | 38.8108 | -120.0348 | South Lake Tahoe approach       |
| `cajon`       | Cajon Pass        | I-15    | 4,190          | 34.3166 | -117.4629 | LA to Las Vegas (I-15)          |
| `tehachapi`   | Tehachapi Pass    | CA-58   | 3,793          | 35.1308 | -118.4383 | Bakersfield to South Mojave     |
| `monitor`     | Monitor Pass      | CA-89   | 8,314          | 38.6783 | -119.5985 | US-395 connector; seasonal      |
| `tioga`       | Tioga Pass        | CA-120  | 9,943          | 37.9099 | -119.2552 | Yosemite east gate; seasonal    |
| `sonora`      | Sonora Pass       | CA-108  | 9,624          | 38.3294 | -119.6264 | Seasonal (Jun–Oct)              |
| `mt-shasta`   | Mt. Shasta Summit | I-5     | 3,540          | 41.4028 | -122.3455 | I-5 north of Redding            |

### Cities to Add to RouteEndpointRegistry

| Id              | Name          | State | Lat     | Lon       |
| --------------- | ------------- | ----- | ------- | --------- |
| `los-angeles`   | Los Angeles   | CA    | 34.0522 | -118.2437 |
| `san-francisco` | San Francisco | CA    | 37.7749 | -122.4194 |
| `sacramento`    | Sacramento    | CA    | 38.5816 | -121.4944 |
| `fresno`        | Fresno        | CA    | 36.7378 | -119.7871 |
| `reno`          | Reno          | NV    | 39.5296 | -119.8138 |
| `las-vegas`     | Las Vegas     | NV    | 36.1699 | -115.1398 |
| `bakersfield`   | Bakersfield   | CA    | 35.3733 | -119.0187 |

### Commit message

```
feat(ca): add California passes and Caltrans data source
```

---

## Wyoming (WY) ⬜

### Data Source

**Agency:** Wyoming DOT (WYDOT)
**Site:** https://wyoroad.info / https://map.wyoroad.info
**API:**

```
# Road conditions by route (text/HTML scrape or structured feed)
GET https://www.wyoroad.info/pls/Browse/WRR.RoutesQuery
    → HTML table; reliable but requires parsing

# WYDOT ArcGIS REST (preferred for structured data)
GET https://services2.arcgis.com/XAiUZpe3bqC8OiNm/arcgis/rest/services/
    WYDOT_Road_Conditions/FeatureServer/0/query
    ?where=1%3D1&outFields=*&f=json

# Camera inventory
GET https://www.wyoroad.info/Highway/webcameras/webcameras.html
    → HTML camera list; also available via 511 camera API

# 511 data feed (NTCIP-based)
GET https://map.wyoroad.info/511-map/api/
    → Undocumented but functional JSON; inspect network traffic at map.wyoroad.info
```

**Auth:** None for public services.
**Note:** WYDOT is moderately documented. The ArcGIS REST service is the most reliable
structured data source. Also check Wyoming Traveler Information API at
https://developer.wyoroad.info/ (if available) for direct JSON endpoints.

**Seasonal closures (very important for WY):**

- Snowy Range (WY-130): Closed ~Oct–May
- Logan Pass / Beartooth Highway: Closed ~Oct–May
- Togwotee Pass: Occasionally closed in winter
  Always surface the official closure URL: https://wyoroad.info/Highway/conditions/RoadClosures.html

### Implementation Plan

Create `WydotService.cs` / `IWydotService.cs` and `WyomingPassDataSource.cs`.
Use the ArcGIS REST service for structured condition data. For cameras, parse
the WYDOT camera HTML list or use Wyoming's 511 open data API.

### Passes to Add

| Pass ID        | Name              | Highway | Elevation (ft) | Lat     | Lon       | Notes                             |
| -------------- | ----------------- | ------- | -------------- | ------- | --------- | --------------------------------- |
| `teton-pass`   | Teton Pass        | WY-22   | 8,431          | 43.4952 | -110.9455 | Jackson Hole west gate            |
| `togwotee`     | Togwotee Pass     | US-287  | 9,658          | 43.7601 | -110.0824 | Jackson to Dubois                 |
| `snowy-range`  | Snowy Range Pass  | WY-130  | 10,847         | 41.3681 | -106.1989 | Seasonal (Oct–May closed)         |
| `south-pass`   | South Pass        | WY-28   | 7,412          | 42.3647 | -108.5905 | Famous Oregon Trail crossing      |
| `powder-river` | Powder River Pass | US-16   | 9,666          | 44.2002 | -107.0838 | Bighorn Mountains crossing        |
| `beartooth`    | Beartooth Pass    | US-212  | 10,947         | 45.0030 | -109.4561 | Seasonal; dramatic alpine highway |

### Cities to Add to RouteEndpointRegistry

| Id             | Name         | State | Lat     | Lon       |
| -------------- | ------------ | ----- | ------- | --------- |
| `casper`       | Casper       | WY    | 42.8666 | -106.3131 |
| `cheyenne`     | Cheyenne     | WY    | 41.1400 | -104.8202 |
| `jackson`      | Jackson      | WY    | 43.4799 | -110.7624 |
| `cody`         | Cody         | WY    | 44.5263 | -109.0565 |
| `rock-springs` | Rock Springs | WY    | 41.5875 | -109.2029 |

### Commit message

```
feat(wy): add Wyoming passes and WYDOT data source
```

---

## Utah (UT) ⬜

### Data Source

**Agency:** Utah DOT (UDOT)
**Site:** https://udottraffic.utah.gov
**API:**

```
# UDOT ArcGIS REST — road conditions
GET https://services.arcgis.com/Vl0VBqVpJSB0FpLN/arcgis/rest/services/
    Road_Conditions/FeatureServer/0/query
    ?where=1%3D1&outFields=*&f=json

# UDOT cameras (open data portal)
GET https://data-uplan.opendata.arcgis.com/datasets/
    traffic-cameras/FeatureServer/0/query
    ?where=1%3D1&outFields=*&f=json

# UDOT 511 open data
GET https://udottraffic.utah.gov/api/v1/
    (requires API key registration at https://udottraffic.utah.gov/developer)
```

**Auth:** ArcGIS public endpoints require no key. UDOT 511 API requires free registration.
**Note:** Utah has excellent ArcGIS-based open data. Primary use case is I-80 corridor
(Salt Lake to Nevada border) and I-15 corridor.

### Passes to Add

| Pass ID          | Name                  | Highway | Elevation (ft) | Lat     | Lon       | Notes                      |
| ---------------- | --------------------- | ------- | -------------- | ------- | --------- | -------------------------- |
| `parleys`        | Parleys Canyon Summit | I-80    | 6,755          | 40.6897 | -111.7437 | Salt Lake to Park City     |
| `soldier-summit` | Soldier Summit        | US-6    | 7,477          | 39.8358 | -110.9294 | Salt Lake to Price         |
| `sardine`        | Sardine Canyon        | US-91   | 5,540          | 41.6347 | -111.8491 | Brigham City to Logan      |
| `cedar-mountain` | Cedar Canyon          | UT-14   | 9,240          | 37.6315 | -112.9621 | Cedar City to Bryce Canyon |
| `beaver-canyon`  | UT-153 Summit         | UT-153  | 9,400          | 38.3980 | -112.4614 | Beaver to Piute Reservoir  |
| `pine-valley`    | Veyo / Black Ridge    | I-15    | 5,545          | 37.2650 | -113.6968 | St. George area            |

### Cities to Add to RouteEndpointRegistry

| Id               | Name           | State | Lat     | Lon       |
| ---------------- | -------------- | ----- | ------- | --------- |
| `salt-lake-city` | Salt Lake City | UT    | 40.7608 | -111.8910 |
| `provo`          | Provo          | UT    | 40.2338 | -111.6585 |
| `ogden`          | Ogden          | UT    | 41.2230 | -111.9738 |
| `st-george`      | St. George     | UT    | 37.1041 | -113.5841 |
| `moab`           | Moab           | UT    | 38.5733 | -109.5498 |

### Commit message

```
feat(ut): add Utah passes and UDOT data source
```

---

## Nevada (NV) ⬜

### Data Source

**Agency:** Nevada DOT (NDOT)
**Site:** https://nvroads.com
**API:**

```
# NV Roads ArcGIS feature service
GET https://services.arcgis.com/8lRhdTsQyJpO52F1/arcgis/rest/services/
    Open511_Incidents/FeatureServer/0/query
    ?where=1%3D1&outFields=*&f=json

# NDOT camera feed
GET https://nvroads.com/cameras/cameras.json
    (inspect network at nvroads.com for current endpoint)

# NDOT 511 API (undocumented but public)
GET https://nvroads.com/api/v1/
```

**Auth:** None for ArcGIS public endpoints.
**Note:** Nevada's pass data coverage is thinner than western mountain states.
OpenWeather fallback recommended for Golconda Summit (I-80) and minor passes.
Spooner Summit and Mt. Rose are better documented (Tahoe region).

### Passes to Add

| Pass ID      | Name            | Highway | Elevation (ft) | Lat     | Lon       | Notes                   |
| ------------ | --------------- | ------- | -------------- | ------- | --------- | ----------------------- |
| `spooner`    | Spooner Summit  | US-50   | 7,146          | 39.1013 | -119.9124 | Lake Tahoe south shore  |
| `mount-rose` | Mt. Rose Summit | NV-431  | 8,911          | 39.3235 | -119.9140 | Reno to Incline Village |
| `golconda`   | Golconda Summit | I-80    | 5,145          | 40.9396 | -117.5048 | I-80 west of Winnemucca |
| `palisade`   | Palisade Canyon | I-80    | 4,540          | 40.5280 | -116.2037 | Elko area               |

### Cities to Add to RouteEndpointRegistry

_(reno and las-vegas added under California section above)_

| Id           | Name       | State | Lat     | Lon       |
| ------------ | ---------- | ----- | ------- | --------- |
| `elko`       | Elko       | NV    | 40.8324 | -115.7631 |
| `winnemucca` | Winnemucca | NV    | 40.9730 | -117.7358 |

### Commit message

```
feat(nv): add Nevada passes and NV Roads data source
```

---

## New Mexico (NM) ⬜

### Data Source

**Agency:** New Mexico DOT (NMDOT)
**Site:** https://nmroads.com
**API:**

```
# NM Roads ArcGIS REST
GET https://nmroads.com/arcgis/rest/services/NMDOT/RoadConditions/
    MapServer/0/query?where=1%3D1&outFields=*&f=json

# NM 511 cameras
GET https://nmroads.com/arcgis/rest/services/NMDOT/cameras/
    MapServer/0/query?where=1%3D1&outFields=*&f=json
```

**Auth:** None for public services.
**Note:** NM passes are lower-elevation than western mountain states so winter
closures are less common, but I-25 (Glorieta Pass) and I-40 (Tijeras) are important
for east-west and north-south routes.

### Passes to Add

| Pass ID         | Name           | Highway | Elevation (ft) | Lat     | Lon       | Notes                     |
| --------------- | -------------- | ------- | -------------- | ------- | --------- | ------------------------- |
| `glorieta`      | Glorieta Pass  | I-25    | 7,432          | 35.5218 | -105.7700 | Albuquerque to Santa Fe   |
| `tijeras`       | Tijeras Canyon | I-40    | 6,534          | 35.0698 | -106.3855 | Albuquerque east approach |
| `raton`         | Raton Pass     | I-25    | 7,834          | 36.9977 | -104.5076 | NM-CO border on I-25      |
| `apache-summit` | Apache Summit  | US-60   | 7,600          | 34.1350 | -109.8490 | Show Low area             |
| `emory`         | Emory Pass     | NM-152  | 8,228          | 32.9019 | -107.6856 | Silver City area          |

### Cities to Add to RouteEndpointRegistry

| Id            | Name        | State | Lat     | Lon       |
| ------------- | ----------- | ----- | ------- | --------- |
| `albuquerque` | Albuquerque | NM    | 35.0844 | -106.6504 |
| `santa-fe`    | Santa Fe    | NM    | 35.6870 | -105.9378 |
| `el-paso`     | El Paso     | TX    | 31.7619 | -106.4850 |

### Commit message

```
feat(nm): add New Mexico passes and NMDOT data source
```

---

## Virginia (VA) ⬜

### Data Source

**Agency:** Virginia DOT (VDOT)
**Site:** https://511Virginia.org
**API:**

```
# VDOT 511 open data (free API key required)
GET https://www.511virginia.org/api/v1/incidents
    ?apiKey={key}
GET https://www.511virginia.org/api/v1/cameras
    ?apiKey={key}

# VDOT Smart Traffic Center (ArcGIS)
GET https://gis.vdot.virginia.gov/arcgis/rest/services/
    PublicSite/VDOT_RoadConditions/MapServer/0/query
    ?where=1%3D1&outFields=*&f=json
```

**Auth:** Free API key from https://developer.511Virginia.org
**Note:** Virginia mountain passes are lower elevation (Appalachians) but I-64 through
Afton Mountain and I-77 at Fancy Gap close regularly in winter ice events.

### Passes to Add

| Pass ID          | Name                     | Highway | Elevation (ft) | Lat     | Lon      | Notes                        |
| ---------------- | ------------------------ | ------- | -------------- | ------- | -------- | ---------------------------- |
| `afton-mountain` | Afton Mountain           | I-64    | 1,929          | 37.9990 | -78.8980 | Charlottesville to Staunton  |
| `rockfish-gap`   | Rockfish Gap             | US-250  | 1,909          | 38.0343 | -78.8682 | Blue Ridge Parkway jct       |
| `fancy-gap`      | Fancy Gap                | I-77    | 2,895          | 36.6623 | -80.6460 | I-77 Blue Ridge crossing     |
| `shenandoah-gap` | Front Royal (Skyline Dr) | US-340  | 1,460          | 38.9201 | -78.1944 | Shenandoah NP north entrance |

### Cities to Add to RouteEndpointRegistry

| Id                | Name            | State | Lat     | Lon      |
| ----------------- | --------------- | ----- | ------- | -------- |
| `richmond`        | Richmond        | VA    | 37.5407 | -77.4360 |
| `roanoke`         | Roanoke         | VA    | 37.2710 | -79.9414 |
| `charlottesville` | Charlottesville | VA    | 38.0293 | -78.4767 |
| `charlotte-nc`    | Charlotte       | NC    | 35.2271 | -80.8431 |
| `knoxville`       | Knoxville       | TN    | 35.9606 | -83.9207 |

### Commit message

```
feat(va): add Virginia Appalachian passes and VDOT data source
```

---

## North Carolina / Tennessee (NC/TN) ⬜

### Data Source

**Agency NC:** NCDOT
**Site:** https://drivenc.gov / https://tims.ncdot.gov
**API:**

```
GET https://tims.ncdot.gov/tims/api/v1/incidents?format=json
```

**Agency TN:** TDOT
**Site:** https://www.tn511.com
**API:**

```
GET https://developer.tn511.com/api/
    (requires free API key registration)
```

**Note:** The Great Smoky Mountains / Blue Ridge region is perhaps the most complex
routing area in the East. Many secondary roads close in ice/snow. US-441 through
Newfound Gap is the primary trans-Smokies route.
GSMNP road closures can be checked at:
https://www.nps.gov/grsm/planyourvisit/parkreads.htm

### Passes to Add

| Pass ID           | Name              | Highway   | Elevation (ft) | Lat     | Lon      | Notes                             |
| ----------------- | ----------------- | --------- | -------------- | ------- | -------- | --------------------------------- |
| `newfound-gap`    | Newfound Gap      | US-441    | 5,046          | 35.6112 | -83.4265 | Great Smoky Mtns, NC/TN border    |
| `cherokee`        | Big Witch Gap     | US-19     | 3,200          | 35.4450 | -83.2680 | Cherokee NC approach              |
| `clinch-mountain` | Clinch Mountain   | US-11W    | 3,074          | 36.5000 | -82.8000 | NE Tennessee                      |
| `santeelah`       | Wayah Bald        | US-64     | 4,180          | 35.1700 | -83.6400 | Western NC                        |
| `clingmans-dome`  | Clingmans Dome Rd | Spur road | 6,643          | 35.5629 | -83.4985 | Seasonal; highest paved road east |

### Cities to Add to RouteEndpointRegistry

| Id            | Name        | State | Lat     | Lon      |
| ------------- | ----------- | ----- | ------- | -------- |
| `asheville`   | Asheville   | NC    | 35.5951 | -82.5515 |
| `chattanooga` | Chattanooga | TN    | 35.0456 | -85.3097 |
| `nashville`   | Nashville   | TN    | 36.1627 | -86.7816 |
| `atlanta`     | Atlanta     | GA    | 33.7490 | -84.3880 |

### Commit message

```
feat(nc-tn): add Smokies/Blue Ridge passes and NC/TN data sources
```

---

## Nationwide City Coverage Expansion ⬜

Beyond state-specific passes, the `RouteEndpointRegistry` needs to cover all major
US cities so any reasonable origin/destination pair works in the UI.

### Additional Major Cities (no pass-specific work needed)

These cities are hubs whose routes will pass through states already covered.
Add them when implementing Phase 0 (dynamic OSRM routing), since a static city-pair
list is no longer needed once routing is dynamic.

| Id              | Name          | State | Lat     | Lon       |
| --------------- | ------------- | ----- | ------- | --------- |
| `phoenix`       | Phoenix       | AZ    | 33.4484 | -112.0740 |
| `tucson`        | Tucson        | AZ    | 32.2226 | -110.9747 |
| `albuquerque`   | Albuquerque   | NM    | 35.0844 | -106.6504 |
| `dallas`        | Dallas        | TX    | 32.7767 | -96.7970  |
| `houston`       | Houston       | TX    | 29.7604 | -95.3698  |
| `kansas-city`   | Kansas City   | MO    | 39.0997 | -94.5786  |
| `chicago`       | Chicago       | IL    | 41.8781 | -87.6298  |
| `minneapolis`   | Minneapolis   | MN    | 44.9778 | -93.2650  |
| `omaha`         | Omaha         | NE    | 41.2565 | -95.9345  |
| `sioux-falls`   | Sioux Falls   | SD    | 43.5446 | -96.7311  |
| `fargo`         | Fargo         | ND    | 46.8772 | -96.7898  |
| `boise`         | Boise         | ID    | 43.6150 | -116.2023 |
| `twin-falls`    | Twin Falls    | ID    | 42.5630 | -114.4609 |
| `portland-me`   | Portland      | ME    | 43.6591 | -70.2568  |
| `boston`        | Boston        | MA    | 42.3601 | -71.0589  |
| `new-york`      | New York      | NY    | 40.7128 | -74.0060  |
| `philadelphia`  | Philadelphia  | PA    | 39.9526 | -75.1652  |
| `pittsburgh`    | Pittsburgh    | PA    | 40.4406 | -79.9959  |
| `cleveland`     | Cleveland     | OH    | 41.4993 | -81.6944  |
| `detroit`       | Detroit       | MI    | 42.3314 | -83.0458  |
| `memphis`       | Memphis       | TN    | 35.1495 | -90.0490  |
| `new-orleans`   | New Orleans   | LA    | 29.9511 | -90.0715  |
| `miami`         | Miami         | FL    | 25.7617 | -80.1918  |
| `orlando`       | Orlando       | FL    | 28.5383 | -81.3792  |
| `tampa`         | Tampa         | FL    | 27.9506 | -82.4572  |
| `jacksonville`  | Jacksonville  | FL    | 30.3322 | -81.6557  |
| `birmingham`    | Birmingham    | AL    | 33.5186 | -86.8104  |
| `oklahoma-city` | Oklahoma City | OK    | 35.4676 | -97.5164  |
| `wichita`       | Wichita       | KS    | 37.6872 | -97.3301  |
| `st-louis`      | St. Louis     | MO    | 38.6270 | -90.1994  |
| `indianapolis`  | Indianapolis  | IN    | 39.7684 | -86.1581  |
| `columbus`      | Columbus      | OH    | 39.9612 | -82.9988  |
| `cincinnati`    | Cincinnati    | OH    | 39.1031 | -84.5120  |
| `louisville`    | Louisville    | KY    | 38.2527 | -85.7585  |
| `lexington`     | Lexington     | KY    | 38.0406 | -84.5037  |
| `baltimore`     | Baltimore     | MD    | 39.2904 | -76.6122  |
| `washington-dc` | Washington DC | DC    | 38.9072 | -77.0369  |
| `norfolk`       | Norfolk       | VA    | 36.8508 | -76.2859  |
| `greenville-sc` | Greenville    | SC    | 34.8526 | -82.3940  |
| `columbia-sc`   | Columbia      | SC    | 34.0007 | -81.0348  |
| `savannah`      | Savannah      | GA    | 32.0809 | -81.0912  |

---

## Phase 0: Routing Architecture (prerequisite for city-to-city search) ⬜

Before iterating through states, these architectural changes are needed so any two cities
can be routed dynamically. This should be its own commit.

### Files to create/modify

| File                              | Type   | Purpose                                 |
| --------------------------------- | ------ | --------------------------------------- |
| `Models/ComputedRoute.cs`         | New    | Route result model returned to frontend |
| `Services/IRoutingService.cs`     | New    | Interface for city-to-city routing      |
| `Services/OsrmRoutingService.cs`  | New    | OSRM HTTP client                        |
| `Services/IPassLocatorService.cs` | New    | Interface for finding passes on route   |
| `Services/PassLocatorService.cs`  | New    | Haversine distance filter               |
| `Controllers/RoutesController.cs` | Modify | Add `GET /api/routes/compute?from=&to=` |
| `Data/RouteEndpointRegistry.cs`   | Modify | Expand to 50+ cities                    |
| `Program.cs`                      | Modify | Register new services                   |

### OSRM call shape

```csharp
// GET https://router.project-osrm.org/route/v1/driving/
//     {originLon},{originLat};{destLon},{destLat}
//     ?alternatives=true&steps=true&geometries=geojson&overview=full&annotations=false

record OsrmRouteResponse(OsrmRoute[] Routes);
record OsrmRoute(double Distance, double Duration, OsrmLeg[] Legs, GeoJsonGeometry Geometry);
record OsrmLeg(OsrmStep[] Steps);
record OsrmStep(string Name, string? Ref, double Distance);
// Ref contains "I-90", "US-2" etc — use to identify which highways the route uses
```

### PassLocatorService logic

```
For each pass P in PassRegistry:
    closestDist = min_distance(P.Lat/Lon, all route polyline segments)
    if closestDist < 15 km → include pass
```

Uses Haversine formula; no external call needed.

### Commit message

```
feat(arch): add OSRM routing service and geometric pass locator
```

---

_Last updated: 2026-02-22_
_Next state to implement: Oregon (OR)_

export interface RouteEndpoint {
  id: string;
  name: string;
  state: string;
  latitude: number;
  longitude: number;
}

/** A named highway route (e.g. I-90 or US-2). */
export interface Route {
  id: string;
  name: string;
  highway: string;
}

export interface SelectedRoute {
  from: RouteEndpoint;
  to: RouteEndpoint;
  highway: string;
}

/** GeoJSON LineString returned from OSRM — coordinates are [lon, lat] pairs. */
export interface RouteGeometry {
  type: 'LineString';
  coordinates: [number, number][];
}

/**
 * A city-to-city route computed by the OSRM routing engine.
 * passIds can be passed to getAllPasses() to fetch full pass details.
 */
export interface ComputedRoute {
  id: string;
  name: string;
  highwaysUsed: string[];
  distanceMiles: number;
  estimatedMinutes: number;
  passIds: string[];
  geometry: RouteGeometry | null;
}

/** Minimal pass info used on the frontend to preview which passes a route crosses. */
export interface PassWaypoint {
  id: string;
  name: string;
  state: string;
  longitude: number;
  highway: string;
}

/**
 * Returns the passes that fall between two endpoints on a specific highway,
 * in trip order.
 */
export function passesOnRoute(
  from: RouteEndpoint,
  to: RouteEndpoint,
  highway: string,
  waypoints: PassWaypoint[],
): PassWaypoint[] {
  const minLon = Math.min(from.longitude, to.longitude);
  const maxLon = Math.max(from.longitude, to.longitude);
  const eastward = to.longitude > from.longitude;
  return waypoints
    .filter(
      (p) =>
        p.highway === highway && p.longitude > minLon && p.longitude < maxLon,
    )
    .sort((a, b) =>
      eastward ? a.longitude - b.longitude : b.longitude - a.longitude,
    );
}

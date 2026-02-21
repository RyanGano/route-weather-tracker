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

/** Minimal pass info used on the frontend to preview which passes a route crosses. */
export interface PassWaypoint {
  id: string;
  name: string;
  state: string;
  longitude: number;
  highway: string;
}

/**
 * All known passes, ordered west to east.
 * Kept in sync with PassRegistry on the backend.
 */
export const PASS_WAYPOINTS: PassWaypoint[] = [
  {
    id: "snoqualmie",
    name: "Snoqualmie Pass",
    state: "WA",
    longitude: -121.4116,
    highway: "I-90",
  },
  {
    id: "stevens-pass",
    name: "Stevens Pass",
    state: "WA",
    longitude: -121.0891,
    highway: "US-2",
  },
  {
    id: "fourth-of-july",
    name: "Fourth of July Pass",
    state: "ID",
    longitude: -116.3667,
    highway: "I-90",
  },
  {
    id: "lookout",
    name: "Lookout Pass",
    state: "MT/ID",
    longitude: -115.699,
    highway: "I-90",
  },
];

/**
 * Returns the passes that fall between two endpoints on a specific highway,
 * in trip order.
 */
export function passesOnRoute(
  from: RouteEndpoint,
  to: RouteEndpoint,
  highway: string,
): PassWaypoint[] {
  const minLon = Math.min(from.longitude, to.longitude);
  const maxLon = Math.max(from.longitude, to.longitude);
  const eastward = to.longitude > from.longitude;
  return PASS_WAYPOINTS.filter(
    (p) =>
      p.highway === highway && p.longitude > minLon && p.longitude < maxLon,
  ).sort((a, b) =>
    eastward ? a.longitude - b.longitude : b.longitude - a.longitude,
  );
}

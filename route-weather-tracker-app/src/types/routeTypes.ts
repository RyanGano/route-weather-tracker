export interface RouteEndpoint {
  id: string;
  name: string;
  state: string;
  latitude: number;
  longitude: number;
}

export interface SelectedRoute {
  from: RouteEndpoint;
  to: RouteEndpoint;
}

/** Minimal pass info used on the frontend to preview which passes a route crosses. */
export interface PassWaypoint {
  id: string;
  name: string;
  state: string;
  longitude: number;
}

/**
 * The known passes along I-90, ordered west to east.
 * Kept in sync with PassRegistry on the backend.
 */
export const PASS_WAYPOINTS: PassWaypoint[] = [
  {
    id: "snoqualmie",
    name: "Snoqualmie Pass",
    state: "WA",
    longitude: -121.4116,
  },
  {
    id: "fourth-of-july",
    name: "Fourth of July Pass",
    state: "ID",
    longitude: -116.3667,
  },
  { id: "lookout", name: "Lookout Pass", state: "MT/ID", longitude: -115.699 },
];

/** Returns the passes that fall between two endpoints, in trip order. */
export function passesOnRoute(
  from: RouteEndpoint,
  to: RouteEndpoint,
): PassWaypoint[] {
  const minLon = Math.min(from.longitude, to.longitude);
  const maxLon = Math.max(from.longitude, to.longitude);
  const eastward = to.longitude > from.longitude;
  return PASS_WAYPOINTS.filter(
    (p) => p.longitude > minLon && p.longitude < maxLon,
  ).sort((a, b) =>
    eastward ? a.longitude - b.longitude : b.longitude - a.longitude,
  );
}

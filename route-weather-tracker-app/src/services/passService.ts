import axios from "axios";
import type { PassSummary } from "../types/passTypes";
import type {
  ComputedRoute,
  Route,
  RouteEndpoint,
  PassWaypoint,
} from "../types/routeTypes";

// Aspire injects VITE_API_URL at runtime with the backend's service-discovered URL.
// Fallback to empty string so the Vite dev-server proxy can also be used.
const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? "";

const api = axios.create({ baseURL: BASE_URL });

/** Returns all known route endpoints (cities), ordered west to east. */
export async function getRouteEndpoints(): Promise<RouteEndpoint[]> {
  const response = await api.get<RouteEndpoint[]>("/api/endpoints");
  return response.data;
}

/** Returns all known highway routes (I-90, US-2, etc.). */
export async function getRoutes(): Promise<Route[]> {
  const response = await api.get<Route[]>("/api/routes");
  return response.data;
}

/** Returns minimal waypoint data (id, name, state, longitude, highway) for all known passes. */
export async function getPassWaypoints(): Promise<PassWaypoint[]> {
  const response = await api.get<PassWaypoint[]>("/api/passes/waypoints");
  return response.data;
}

/**
 * Returns passes for the given route.
 * Pass `from` and `to` endpoint IDs to get only the passes between those cities
 * in trip order. Pass `highway` to restrict to a specific highway corridor.
 * Omit all params to get every known pass.
 */
export async function getAllPasses(
  from?: string,
  to?: string,
  highway?: string,
): Promise<PassSummary[]> {
  const params: Record<string, string> = {};
  if (from) params.from = from;
  if (to) params.to = to;
  if (highway) params.highway = highway;
  const response = await api.get<PassSummary[]>("/api/passes", { params });
  return response.data;
}

/**
 * Computes up to three driving route options between two endpoint IDs using
 * the OSRM routing engine. Each route includes the pass IDs matched along its
 * geometry. Returns an empty array if OSRM is unreachable.
 */
export async function computeRoutes(
  from: string,
  to: string,
): Promise<ComputedRoute[]> {
  const response = await api.get<ComputedRoute[]>("/api/routes/compute", {
    params: { from, to },
  });
  return response.data;
}

/**
 * Fetches full PassSummary objects for a list of pass IDs returned by computeRoutes.
 * Calls the existing /api/passes endpoint with each ID in parallel.
 */
export async function getPassesByIds(passIds: string[]): Promise<PassSummary[]> {
  if (passIds.length === 0) return [];
  const results = await Promise.all(
    passIds.map((id) =>
      api
        .get<PassSummary>(`/api/passes/${id}`)
        .then((r) => r.data)
        .catch(() => null),
    ),
  );
  return results.filter((p): p is PassSummary => p !== null);
}

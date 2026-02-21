import axios from "axios";
import type { PassSummary } from "../types/passTypes";
import type { RouteEndpoint } from "../types/routeTypes";

// Aspire injects VITE_API_URL at runtime with the backend's service-discovered URL.
// Fallback to empty string so the Vite dev-server proxy can also be used.
const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? "";

const api = axios.create({ baseURL: BASE_URL });

/** Returns all known route endpoints (cities), ordered west to east. */
export async function getRouteEndpoints(): Promise<RouteEndpoint[]> {
  const response = await api.get<RouteEndpoint[]>("/api/endpoints");
  return response.data;
}

/**
 * Returns passes for the given route.
 * Pass `from` and `to` endpoint IDs to get only the passes between those cities
 * in trip order. Omit both to get all passes.
 */
export async function getAllPasses(
  from?: string,
  to?: string,
): Promise<PassSummary[]> {
  const params: Record<string, string> = {};
  if (from) params.from = from;
  if (to) params.to = to;
  const response = await api.get<PassSummary[]>("/api/passes", { params });
  return response.data;
}

export async function getPassById(id: string): Promise<PassSummary> {
  const response = await api.get<PassSummary>(`/api/passes/${id}`);
  return response.data;
}

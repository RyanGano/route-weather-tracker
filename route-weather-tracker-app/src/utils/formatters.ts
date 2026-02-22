import type { RouteEndpoint } from "../types/routeTypes";
import type { TravelRestriction as TravelRestrictionType } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";

/** Returns a display label for a route endpoint: "City, State". */
export function endpointLabel(ep: RouteEndpoint): string {
  return `${ep.name}, ${ep.state}`;
}

/**
 * Returns a human-readable label for a travel restriction.
 * Prefers the raw source text (which may include qualifiers like "Advised")
 * over the generic enum-derived fallback.
 */
export function formatRestriction(
  restriction: TravelRestrictionType,
  text?: string,
): string {
  if (text) return text;
  switch (restriction) {
    case TravelRestriction.Closed:
      return "Closed";
    case TravelRestriction.ChainsRequired:
      return "Chains Required";
    case TravelRestriction.TiresOrTraction:
      return "Traction Tires Required";
    default:
      return "No Restrictions";
  }
}

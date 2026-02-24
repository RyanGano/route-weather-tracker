import type { RouteEndpoint, ComputedRoute } from "../types/routeTypes";
import type { TravelRestriction as TravelRestrictionType } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";

/** Returns a display label for a route endpoint: "City, State". */
export function endpointLabel(ep: RouteEndpoint): string {
  return `${ep.name}, ${ep.state}`;
}

/**
 * Converts a ComputedRoute into a URL-safe slug derived from its highways,
 * e.g. ["I-90", "US-2"] → "i-90-us-2".
 */
export function routeToSlug(route: ComputedRoute): string {
  return route.highwaysUsed
    .map((h) => h.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, ""))
    .join("-");
}

/** Returns true when a route's slug matches the given URL slug. */
export function routeMatchesSlug(route: ComputedRoute, slug: string): boolean {
  return routeToSlug(route) === slug;
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

import { useAdConfig } from "../contexts/AdContext";
import { getContextualOffer } from "../utils/adContextUtils";
import ContextualOfferCard from "./ContextualOfferCard";
import AdSlot from "./AdSlot";
import type { PassSummary } from "../types/passTypes";
import type { RouteEndpoint } from "../types/routeTypes";
import type { ComputedRoute } from "../types/routeTypes";

interface Props {
  passes: PassSummary[];
  destination: RouteEndpoint | null;
  route: ComputedRoute | null;
}

/**
 * Shows a contextual affiliate card (Amazon/Booking.com) when a route/pass
 * signal matches a trigger. Falls back to a Google AdSense unit when no
 * match is found and an ad unit ID is configured. Renders nothing when ads
 * are disabled.
 */
export default function AdBanner({ passes, destination, route }: Props) {
  const config = useAdConfig();

  // Do not render any ad-related UI until the user has selected a route.
  // This ensures we don't load ad scripts or units when the app is on the
  // home/landing view and the user hasn't picked a route yet.
  if (!route) return null;

  if (!config.adsEnabled) return null;

  const offer = getContextualOffer(passes, destination, route, config);

  if (offer) {
    return <ContextualOfferCard offer={offer} />;
  }

  if (config.adsensePublisherId && config.adsenseAdUnitId) {
    return (
      <AdSlot
        publisherId={config.adsensePublisherId}
        adUnitId={config.adsenseAdUnitId}
      />
    );
  }

  return null;
}

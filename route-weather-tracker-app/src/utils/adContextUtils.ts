import type { PassSummary } from "../types/passTypes";
import { TravelRestriction } from "../types/passTypes";
import type { RouteEndpoint } from "../types/routeTypes";
import type { ComputedRoute } from "../types/routeTypes";
import type { AdConfig, AffiliateOffer } from "../types/adTypes";

function hasSnowForecast(pass: PassSummary): boolean {
  if (!pass.weather) return false;
  const forecasts = pass.weather.dailyForecasts.slice(0, 3);
  return forecasts.some((f) => {
    const d = f.description.toLowerCase();
    return (
      d.includes("snow") ||
      d.includes("sleet") ||
      d.includes("blizzard") ||
      f.iconCode.startsWith("13")
    );
  });
}

function isSkiRegion(pass: PassSummary): boolean {
  if (pass.info.elevationFeet < 5000) return false;
  const d = (
    pass.weather?.currentDescription ??
    pass.condition?.weatherCondition ??
    ""
  ).toLowerCase();
  return (
    d.includes("snow") ||
    d.includes("sleet") ||
    d.includes("blizzard") ||
    hasSnowForecast(pass)
  );
}

/**
 * Returns the highest-priority contextual affiliate offer for the full route
 * banner. Returns null when no signal is triggered (AdSense fills instead).
 * Priority: chains → cold temp → snow forecast → ski region → destination
 * hotel → long trip lodging.
 */
export function getContextualOffer(
  passes: PassSummary[],
  destination: RouteEndpoint | null,
  route: ComputedRoute | null,
  config: AdConfig,
): AffiliateOffer | null {
  if (!config.adsEnabled) return null;

  const tag = config.amazonTag;
  const aid = config.bookingAid;

  // 1 — Chains required at any pass
  const chainsPass = passes.find(
    (p) =>
      p.condition &&
      (p.condition.eastboundRestriction >= TravelRestriction.ChainsRequired ||
        p.condition.westboundRestriction >= TravelRestriction.ChainsRequired),
  );
  if (chainsPass && tag) {
    return {
      provider: "amazon",
      emoji: "⛓️",
      headline: `Chains required near ${chainsPass.info.name}`,
      subtext:
        "Make sure you're equipped — shop tire chains and traction devices.",
      url: `https://www.amazon.com/s?k=tire+chains+traction+cables&tag=${tag}`,
    };
  }

  // 2 — Below freezing at any pass
  const coldPass = passes.find(
    (p) => p.condition && p.condition.temperatureFahrenheit < 32,
  );
  if (coldPass && tag) {
    return {
      provider: "amazon",
      emoji: "🥶",
      headline: `Below freezing at ${coldPass.info.name}`,
      subtext:
        "Stay warm on the road — hand warmers, gloves, and cold-weather essentials.",
      url: `https://www.amazon.com/s?k=cold+weather+driving+gear+hand+warmers&tag=${tag}`,
    };
  }

  // 3 — Snow in the forecast
  const snowPass = passes.find(hasSnowForecast);
  if (snowPass && tag) {
    return {
      provider: "amazon",
      emoji: "❄️",
      headline: `Snow forecast near ${snowPass.info.name}`,
      subtext:
        "Prepare for winter driving — ice scrapers, emergency kits, and more.",
      url: `https://www.amazon.com/s?k=winter+driving+emergency+kit+ice+scraper&tag=${tag}`,
    };
  }

  // 4 — Ski region (high elevation + snow conditions)
  const skiPass = passes.find(isSkiRegion);
  if (skiPass && tag) {
    return {
      provider: "amazon",
      emoji: "⛷️",
      headline: `Ski country on your route`,
      subtext: `${skiPass.info.name} is in ski country — browse ski and outdoor winter gear.`,
      url: `https://www.amazon.com/s?k=ski+gear+winter+outdoor+resort&tag=${tag}`,
    };
  }

  // 5 — Destination hotel
  if (destination && aid) {
    return {
      provider: "booking",
      emoji: "🏨",
      headline: `Find hotels in ${destination.name}`,
      subtext: "Book accommodation at your destination.",
      url: `https://www.booking.com/searchresults.html?ss=${encodeURIComponent(destination.name)}&aid=${aid}`,
    };
  }

  // 6 — Long trip (> 3 hours)
  if (route && route.estimatedMinutes > 180 && destination && aid) {
    return {
      provider: "booking",
      emoji: "🛏️",
      headline: `Long drive ahead — ${Math.round(route.estimatedMinutes / 60)}+ hours`,
      subtext: "Consider breaking the trip up. Find hotels along your route.",
      url: `https://www.booking.com/searchresults.html?ss=${encodeURIComponent(destination.name)}&aid=${aid}`,
    };
  }

  return null;
}

/**
 * Returns a pass-level micro-offer for inline display inside a PassCard.
 * Only triggers on chains or freezing conditions specific to that pass.
 */
export function getPassOffer(
  pass: PassSummary,
  config: AdConfig,
): AffiliateOffer | null {
  if (!config.adsEnabled || !config.amazonTag) return null;

  const tag = config.amazonTag;

  if (
    pass.condition &&
    (pass.condition.eastboundRestriction >= TravelRestriction.ChainsRequired ||
      pass.condition.westboundRestriction >= TravelRestriction.ChainsRequired)
  ) {
    return {
      provider: "amazon",
      emoji: "⛓️",
      headline: "Shop tire chains on Amazon",
      subtext: "",
      url: `https://www.amazon.com/s?k=tire+chains+traction+cables&tag=${tag}`,
    };
  }

  if (pass.condition && pass.condition.temperatureFahrenheit < 32) {
    return {
      provider: "amazon",
      emoji: "🥶",
      headline: "Shop cold-weather gear on Amazon",
      subtext: "",
      url: `https://www.amazon.com/s?k=cold+weather+driving+gear&tag=${tag}`,
    };
  }

  return null;
}

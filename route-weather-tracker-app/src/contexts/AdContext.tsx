import { createContext, useContext, type ReactNode } from "react";
import type { AdConfig } from "../types/adTypes";

const AdContext = createContext<AdConfig>({
  adsEnabled: false,
  amazonTag: "",
  bookingAid: "",
  adsensePublisherId: "",
  adsenseAdUnitId: "",
});

export function AdProvider({ children }: { children: ReactNode }) {
  const config: AdConfig = {
    adsEnabled: import.meta.env.VITE_ADS_ENABLED === "true",
    amazonTag: import.meta.env.VITE_AMAZON_ASSOCIATE_TAG ?? "",
    bookingAid: import.meta.env.VITE_BOOKING_AFFILIATE_ID ?? "",
    adsensePublisherId: import.meta.env.VITE_ADSENSE_PUBLISHER_ID ?? "",
    adsenseAdUnitId: import.meta.env.VITE_ADSENSE_AD_UNIT_ID ?? "",
  };

  return <AdContext.Provider value={config}>{children}</AdContext.Provider>;
}

export function useAdConfig(): AdConfig {
  return useContext(AdContext);
}

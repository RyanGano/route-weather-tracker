import { createContext, useContext } from "react";
import type { AdConfig } from "../types/adTypes";

export const AdContext = createContext<AdConfig>({
  adsEnabled: false,
  amazonTag: "",
  bookingAid: "",
  adsensePublisherId: "",
  adsenseAdUnitId: "",
});

export function useAdConfig(): AdConfig {
  return useContext(AdContext);
}

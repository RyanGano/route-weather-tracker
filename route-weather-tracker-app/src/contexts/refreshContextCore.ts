import { createContext, useContext } from "react";
import type { RefreshState } from "./refreshConstants";

export const RefreshContext = createContext<RefreshState>({
  cacheBuster: Date.now(),
  lastUpdated: new Date(),
});

export function useRefresh(): RefreshState {
  return useContext(RefreshContext);
}
